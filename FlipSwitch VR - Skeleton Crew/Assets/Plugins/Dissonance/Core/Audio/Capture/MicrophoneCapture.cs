using System;
using System.Linq;
using Dissonance.Config;
using Dissonance.Datastructures;
using Dissonance.VAD;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Capture
{
    internal class MicrophoneCapture
        : IDisposable, IMicrophoneCapture
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(MicrophoneCapture).Name);

        private readonly byte _maxReadBufferPower;
        private readonly POTBuffer _readBuffer = new POTBuffer(10);
        private readonly BufferedSampleProvider _rawMicSamples;
        private readonly IFrameProvider _rawMicFrames;

        private AudioFileWriter _microphoneDiagnosticOutput;

        public WaveFormat Format
        {
            get { return _preprocessing.OutputFormat; }
        }

        private readonly IPreprocessingPipeline _preprocessing;

        [CanBeNull] private readonly string _micName;
        private readonly AudioClip _clip;
        private int _readHead;
        private bool _started;
        private bool _audioDeviceChanged;

        public int FrameSize
        {
            get { return _preprocessing.OutputFrameSize; }
        }

        public float Amplitude
        {
            get { return _preprocessing.Amplitude; }
        }
        #endregion

        /// <param name="micName"></param>
        /// <param name="source">Source to read frames from</param>
        private MicrophoneCapture([CanBeNull] string micName, AudioClip source)
        {
            if (source == null)
                throw new ArgumentNullException("source", Log.PossibleBugMessage("capture source clip is null", "333E11A6-8026-41EB-9B34-EF9ADC54B651"));

            AudioSettings.OnAudioConfigurationChanged += OnAudioDeviceChanged;

            _micName = micName;
            _clip = source;
            var captureFormat = new WaveFormat(1, source.frequency);

            _maxReadBufferPower = (byte)Math.Ceiling(Math.Log(0.1f * source.frequency, 2));

            _preprocessing = new WebRtcPreprocessingPipeline(captureFormat);
            _preprocessing.Start();

            //Ensure we have enough buffer size to contain several input frames to the preprocessor
            _rawMicSamples = new BufferedSampleProvider(captureFormat, _preprocessing.InputFrameSize * 4);
            _rawMicFrames = new SampleToFrameProvider(_rawMicSamples, (uint)_preprocessing.InputFrameSize);

            Log.Info("Began mic capture (SampleRate:{0} FrameSize:{1}, Buffer Limit:2^{2})", captureFormat.SampleRate, _preprocessing.InputFrameSize, _maxReadBufferPower);
        }

        [CanBeNull] public static MicrophoneCapture Start([CanBeNull] string micName)
        {
            //Early exit - check if there are no microphones connected
            if (Microphone.devices.Length == 0)
            {
                Log.Warn("No microphone detected; disabling voice capture");
                return null;
            }

            //Check the micName and default to null if it's invalid (all whitespace or not a known device)
            if (string.IsNullOrEmpty(micName))
                micName = null;
            else if (!Microphone.devices.Contains(micName))
            {
                Log.Warn("Cannot find mic '{0}', using default mic", micName);
                micName = null;
            }

            //Get device caps and modify sample rate and frame size to match
            int minFreq;
            int maxFreq;
            Microphone.GetDeviceCaps(micName, out minFreq, out maxFreq);

            //Get device capabilities and choose a sample rate as close to 48000 as possible. If min and max are both zero that indicates we can use any sample rate
            var sampleRate = minFreq == 0 && maxFreq == 0 ? 48000 : Mathf.Clamp(48000, minFreq, maxFreq);

            var clip = Microphone.Start(micName, true, 10, sampleRate);
            if (clip == null)
            {
                Log.Error("Failed to start microphone capture");
                return null;
            }

            var capture = new MicrophoneCapture(micName, clip);
            return capture;
        }

        private void OnAudioDeviceChanged(bool deviceWasChanged)
        {
            _audioDeviceChanged |= deviceWasChanged;
        }

        #region subscriptions
        public void Subscribe(IMicrophoneHandler listener)
        {
            _preprocessing.Subscribe(listener);
        }

        public bool Unsubscribe(IMicrophoneHandler listener)
        {
            return _preprocessing.Unsubscribe(listener);
        }

        public void Subscribe(IVoiceActivationListener listener)
        {
            _preprocessing.Subscribe(listener);
        }

        public bool Unsubscribe(IVoiceActivationListener listener)
        {
            return _preprocessing.Unsubscribe(listener);
        }
        #endregion

        #region main thread audio processing
        //These methods run on the main thread, they drain audio from the mic and send it across to the other thread for proper processing
        //
        // Update            - Either discard mic samples (if no one is subscribed to mic data) or call DrainMicSamples
        // DrainMicSamples   - Read as many samples as possible from the mic (using a set of pow2 sized buffers to get as
        //                     much as possible), passes samples to ConsumeMicSamples
        // ConsumeMicSamples - Take some number of samples from produced by DrainMicSamples and buffer them up, call SendFrame
        //                     every time some samples are added to the buffer
        // SendFrame         - Read as many frames as possible from the buffer and send them to the other thread. Also poke the other thread
        //                     to tell it that it has work to do

        public bool Update()
        {
            if (!_started)
            {
                _readHead = Microphone.GetPosition(_micName);
                _started = _readHead > 0;

                if (!_started)
                    return false;
            }

            //Cause a mic reset if:
            // - The sample count is zero that's an error
            // - The device is changed we really have no idea what state we're in any more
            if (_clip.samples == 0 || _audioDeviceChanged)
                return true;

            if (_preprocessing.RequiresInput)
            {
                //Read samples from clip into mic sample buffer
                DrainMicSamples();
            }
            else
            {
                //We're not interested in the audio from the mic, so skip the read head to the current mic position and drop all the audio
                _readHead = Microphone.GetPosition(_micName);
                _rawMicSamples.Reset();
                _rawMicFrames.Reset();
                _preprocessing.Reset();

                if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.Dispose();
                    _microphoneDiagnosticOutput = null;
                }
            }

            return false;
        }

        private void DrainMicSamples()
        {
            // How many samples has the mic moved since the last time we read from it?
            var writeHead = Microphone.GetPosition(_micName);
            var samplesToRead = (uint)((_clip.samples + writeHead - _readHead) % _clip.samples);

            if (samplesToRead == 0)
                return;

            //If we're trying to read more data than we have buffer space expand the buffer (up to a max limit)
            //If we're at the max limit, just clamp to buffer size and discard the extra samples
            while (samplesToRead > _readBuffer.MaxCount)
            {
                //absolute max buffer size, we will refuse to expand beyond this
                if (_readBuffer.Pow2 > _maxReadBufferPower || !_readBuffer.Expand())
                {
                    Log.Warn(string.Format("Insufficient buffer space, requested {0}, clamped to {1} (dropping samples)", samplesToRead, _readBuffer.MaxCount));

                    //Read as many samples as possible with the limited buffer space available. Skip the head forwards to skip samples we can't read
                    samplesToRead = _readBuffer.MaxCount;

                    var skip = samplesToRead - _readBuffer.MaxCount;
                    _readHead = (int)((_readHead + skip) % _clip.samples);

                    break;
                }
                else
                {
                    Log.Debug(string.Format("Trying to read {0} samples, growing read buffer space to {1}", samplesToRead, _readBuffer.MaxCount));
                }
            }

            //Inform the buffer how many samples we want to read
            _readBuffer.Alloc(samplesToRead);
            try
            {
                while (samplesToRead > 0)
                {
                    //Read from mic
                    var buffer = _readBuffer.GetBuffer(ref samplesToRead, true);
                    _clip.GetData(buffer, _readHead);
                    _readHead = (_readHead + buffer.Length) % _clip.samples;

                    //Send samples downstream
                    ConsumeSamples(new ArraySegment<float>(buffer, 0, buffer.Length));
                }
            }
            finally
            {
                _readBuffer.Free();
            }
        }

        /// <summary>
        /// Given some samples consume them (as many as possible at a time) and send frames downstream (as frequently as possible)
        /// </summary>
        /// <param name="samples"></param>
        private void ConsumeSamples(ArraySegment<float> samples)
        {
            while (samples.Count > 0)
            {
                //Write as many samples as possible (up to capacity of buffer)
                var written = _rawMicSamples.Write(samples.Array, samples.Offset, samples.Count);
                samples = new ArraySegment<float>(samples.Array, samples.Offset + written, samples.Count - written);

                //Drain as many of those samples as possible in frame sized chunks
                SendFrame();
            }
        }

        /// <summary>
        /// Read as many frames as possible from the mic sample buffer and pass them to the encoding thread
        /// </summary>
        private void SendFrame()
        {
            while (_rawMicSamples.Count > _preprocessing.InputFrameSize)
            {
                //Get an empty buffer from the pool of buffers (sent back from the audio processing thread)
                var frameBuffer = _preprocessing.GetFrameBuffer();

                //Read a complete frame
                _rawMicFrames.Read(new ArraySegment<float>(frameBuffer));

                //Create diagnostic writer (if necessary)
                if (DebugSettings.Instance.EnableRecordingDiagnostics && DebugSettings.Instance.RecordMicrophoneRawAudio)
                {
                    if (_microphoneDiagnosticOutput == null)
                    {
                        var filename = string.Format("Dissonance_Diagnostics/MicrophoneRawAudio_{0}", DateTime.UtcNow.ToFileTime());
                        _microphoneDiagnosticOutput = new AudioFileWriter(filename, _rawMicSamples.WaveFormat);
                    }
                }
                else if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.Dispose();
                    _microphoneDiagnosticOutput = null;
                }

                //Write out the diagnostic info
                if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.WriteSamples(new ArraySegment<float>(frameBuffer));
                    _microphoneDiagnosticOutput.Flush();
                }

                //Send the full buffer to the audio thread for processing (no copying, just pass the entire buffer across by ref)
                _preprocessing.Send(frameBuffer);
            }
        }
        #endregion

        #region disposal
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _preprocessing.Dispose();
                Microphone.End(_micName);

                if (_microphoneDiagnosticOutput != null)
                {
                    _microphoneDiagnosticOutput.Dispose();
                    _microphoneDiagnosticOutput = null;
                }

                Log.Debug("Stopping audio capture thread");
            }
            _disposed = true;
        }
        #endregion
    }
}