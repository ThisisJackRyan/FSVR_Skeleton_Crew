using System;
using System.Collections.Generic;
using System.Threading;
using Dissonance.Datastructures;
using Dissonance.Threading;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal abstract class BasePreprocessingPipeline
        : IPreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(BasePreprocessingPipeline).Name);

        private ArvCalculator _arv = new ArvCalculator();
        public float Amplitude
        {
            get { return _arv.ARV; }
        }

        private int _droppedFrames;
        private readonly float[] _emptyInputFrame;
        private readonly ConcurrentPool<float[]> _inputBufferSource;
        private readonly TransferBuffer<float[]> _inputQueue;

        private readonly BufferedSampleProvider _resamplerInput;
        private readonly Resampler _resampler;
        private readonly SampleToFrameProvider _resampledOutput;
        private readonly float[] _intermediateFrame;

        private readonly int _inputFrameSize;
        public int InputFrameSize
        {
            get { return _inputFrameSize; }
        }

        private readonly int _outputFrameSize;
        public int OutputFrameSize
        {
            get { return _outputFrameSize; }
        }

        private readonly WaveFormat _outputFormat;
        [NotNull] public WaveFormat OutputFormat { get { return _outputFormat; } }

        public abstract bool RequiresInput { get; }

        private bool _resetApplied;
        private int _resetRequested = 1;

        private volatile bool _runThread;
        private readonly DThread _thread;
        private readonly AutoResetEvent _threadEvent;

        private readonly ReadonlyLockedValue<List<IMicrophoneHandler>> _micSubscriptions = new ReadonlyLockedValue<List<IMicrophoneHandler>>(new List<IMicrophoneHandler>());
        private int _micSubscriptionCount;
        protected int MicSubscriptionCount { get { return _micSubscriptionCount; } }

        private readonly ReadonlyLockedValue<List<IVoiceActivationListener>> _vadSubscriptions = new ReadonlyLockedValue<List<IVoiceActivationListener>>(new List<IVoiceActivationListener>());
        private int _vadSubscriptionCount;
        protected int VadSubscriptionCount { get { return _vadSubscriptionCount; } }

        protected abstract bool VadIsSpeechDetected { get; }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputFormat">format of audio supplied to `Send`</param>
        /// <param name="inputFrameSize">Size of frames which will should be provided from `GetFrameBuffer`</param>
        /// <param name="intermediateFrameSize">Size of frames which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="intermediateSampleRate">Sample rate which should be passed into `PreprocessAudioFrame`</param>
        /// <param name="outputFrameSize">Size of frames which will be provided sent to `SendSamplesToSubscribers`</param>
        /// <param name="outputSampleRate"></param>
        protected BasePreprocessingPipeline(WaveFormat inputFormat, int inputFrameSize, int intermediateFrameSize, int intermediateSampleRate, int outputFrameSize, int outputSampleRate)
        {
            if (inputFrameSize < 0)
                throw new ArgumentOutOfRangeException("inputFrameSize", "Input frame size cannot be less than zero");

            _inputFrameSize = inputFrameSize;
            _outputFrameSize = outputFrameSize;
            _outputFormat = new WaveFormat(1, outputSampleRate);

            //Create input system (source of empty buffers, queue of waiting input data)
            _inputBufferSource = new ConcurrentPool<float[]>(24, () => new float[inputFrameSize]);
            _inputQueue = new TransferBuffer<float[]>(12);
            _emptyInputFrame = new float[inputFrameSize];

            //Create resampler to resample input to intermediate rate
            _resamplerInput = new BufferedSampleProvider(inputFormat, InputFrameSize * 4);
            _resampler = new Resampler(_resamplerInput, 48000);
            _resampledOutput = new SampleToFrameProvider(_resampler, (uint)OutputFrameSize);
            _intermediateFrame = new float[intermediateFrameSize];

            _threadEvent = new AutoResetEvent(false);
            _thread = new DThread(ThreadEntry);
        }

        public virtual void Dispose()
        {
            _runThread = false;
            _threadEvent.Set();
            _thread.Join();

            using (var subscriptions = _micSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            using (var subscriptions = _vadSubscriptions.Lock())
                while (subscriptions.Value.Count > 0)
                    Unsubscribe(subscriptions.Value[0]);

            Log.Info("Disposed pipeline");
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _resetRequested, 1);
        }

        protected virtual void ApplyReset()
        {
            Log.Debug("Resetting preprocessing pipeline");

            _resamplerInput.Reset();
            _resampler.Reset();
            _resampledOutput.Reset();

            _arv.Reset();
            _droppedFrames = 0;

            _resetApplied = true;
        }

        #region frame pooling
        public float[] GetFrameBuffer()
        {
            return _inputBufferSource.Get();
        }

        public void Send(float[] frame)
        {
            if (!_inputQueue.Write(frame))
            {
                Log.Warn("Failed to write microphone samples into input queue");

                //Increment a counter to inform the pipeline that a frame was lost
                Interlocked.Increment(ref _droppedFrames);

                //We failed to process this frame, recycle the buffer
                _inputBufferSource.Put(frame);
            }
            _threadEvent.Set();
        }
        #endregion

        public void Start()
        {
            _runThread = true;
            _thread.Start();
        }

        private void ThreadEntry()
        {
            try
            {
                while (_runThread)
                {
                    //Wait for an event(s) to arrive which we need to process.
                    //Max wait time is the size of the smallest frame size so it should wake up with no work to do
                    // a lot of the time (ensuring minimal processing latency when there is work to do).
                    if (_inputQueue.EstimatedUnreadCount == 0)
                        _threadEvent.WaitOne(10);

                    //Apply a pipeline reset if one has been requested, but one has not yet been applied
                    if (Interlocked.Exchange(ref _resetRequested, 0) == 1 && !_resetApplied)
                        ApplyReset();

                    //If there are no items in the buffer skip back to sleep
                    var countInBuffer = _inputQueue.EstimatedUnreadCount;
                    if (countInBuffer == 0)
                        continue;

                    //We're about to process some audio, meaning the pipeline is no longer in a reset state
                    _resetApplied = false;

                    //Reset the external dropped frames counter and store it locally, we're about to process the packet before the drop
                    var missed = Interlocked.Exchange(ref _droppedFrames, 0);

                    for (var i = 0; i < countInBuffer; i++)
                    {
                        float[] buffer;
                        if (!_inputQueue.Read(out buffer))
                        {
                            Log.Warn("Attempting to drain '{0}' frames from preprocessor input queue, but failed to read frame '{1}'", countInBuffer, i);
                            missed++;
                        }
                        else if (buffer == null)
                        {
                            Log.Warn("Attempting to drain '{0}' frames from preprocessor input queue, but read a null frame for frame '{1}'", countInBuffer, i);
                            missed++;
                        }
                        else
                        {
                            //Run through the preprocessor (rewrite the buffer in place)
                            var preSpeech = VadIsSpeechDetected;
                            ProcessInputAudio(buffer);
                            var postSpeech = VadIsSpeechDetected;

                            //Measure the amplitude of the preprocessed signal
                            _arv.Update(new ArraySegment<float>(buffer));

                            //Update the VAD subscribers if necessary
                            if (preSpeech ^ postSpeech)
                            {
                                if (preSpeech)
                                    SendStoppedTalking();
                                else
                                    SendStartedTalking();
                            }

                            _inputBufferSource.Put(buffer);
                        }
                    }

                    //Now submit as many empty frames as necessary to make up for lost frames
                    for (var i = 0; i < missed; i++)
                    {
                        //We can't get a buffer from the input queue, submit silent frames to make up for lost data
                        Log.Debug("Sending a silent frame to compensate for lost audio from the microphone", missed);
                        Array.Clear(_emptyInputFrame, 0, _emptyInputFrame.Length);

                        ProcessInputAudio(_emptyInputFrame);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(Log.PossibleBugMessage("Unhandled exception killed the microphone capture thread: " + e, "02EB75C0-1E12-4109-BFD2-64645C14BD5F"));
            }
        }

        private void ProcessInputAudio([NotNull] float[] frame)
        {
            //Push frame through the resampler and process the resampled audio one frame at a time
            var offset = 0;
            while (offset != frame.Length)
            {
                offset += _resamplerInput.Write(frame, offset, frame.Length - offset);

                //Read resampled data and push it through the pipeline
                while (_resampledOutput.Read(new ArraySegment<float>(_intermediateFrame, 0, _intermediateFrame.Length)))
                    PreprocessAudioFrame(_intermediateFrame);
            }
        }

        protected abstract void PreprocessAudioFrame(float[] frame);

        #region subscriptions
        protected void SendSamplesToSubscribers(float[] buffer)
        {
            using (var unlocker = _micSubscriptions.Lock())
            {
                var subs = unlocker.Value;
                for (var i = 0; i < subs.Count; i++)
                {
                    try
                    {
                        subs[i].Handle(new ArraySegment<float>(buffer), OutputFormat);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Microphone subscriber '{0}' threw: {1}", subs[i].GetType().Name, ex);
                    }
                }
            }
        }

        public virtual void Subscribe(IMicrophoneHandler listener)
        {
            using (var subscriptions = _micSubscriptions.Lock())
            {
                subscriptions.Value.Add(listener);
                Interlocked.Increment(ref _micSubscriptionCount);
            }
        }

        public virtual bool Unsubscribe(IMicrophoneHandler listener)
        {
            using (var subscriptions = _micSubscriptions.Lock())
            {
                var removed = subscriptions.Value.Remove(listener);
                if (removed)
                    Interlocked.Decrement(ref _micSubscriptionCount);
                return removed;
            }
        }

        private void SendStoppedTalking()
        {
            using (var subs = _vadSubscriptions.Lock())
                for (var i = 0; i < subs.Value.Count; i++)
                    subs.Value[i].VoiceActivationStop();
        }

        private void SendStartedTalking()
        {
            using (var subs = _vadSubscriptions.Lock())
                for (var i = 0; i < subs.Value.Count; i++)
                    subs.Value[i].VoiceActivationStart();
        }

        public virtual void Subscribe(IVoiceActivationListener listener)
        {
            using (var subs = _vadSubscriptions.Lock())
            {
                subs.Value.Add(listener);
                Interlocked.Increment(ref _vadSubscriptionCount);

                if (VadIsSpeechDetected)
                    listener.VoiceActivationStart();
            }
        }

        public virtual bool Unsubscribe(IVoiceActivationListener listener)
        {
            using (var subs = _vadSubscriptions.Lock())
            {
                var removed = subs.Value.Remove(listener);
                if (removed)
                {
                    Interlocked.Decrement(ref _vadSubscriptionCount);

                    if (VadIsSpeechDetected)
                        listener.VoiceActivationStop();
                }
                return removed;
            }
        }
        #endregion
    }
}
