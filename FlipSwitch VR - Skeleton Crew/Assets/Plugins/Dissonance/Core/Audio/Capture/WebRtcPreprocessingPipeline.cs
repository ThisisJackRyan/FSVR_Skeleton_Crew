using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Dissonance.Config;
using Dissonance.Threading;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal class WebRtcPreprocessingPipeline
        : BasePreprocessingPipeline
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(WebRtcPreprocessingPipeline).Name);

        /// <summary>
        /// This pipeline always requires input because it's always running AEC and VAD
        /// </summary>
        public override bool RequiresInput
        {
            get { return true; }
        }

        private bool _isVadDetectingSpeech;
        protected override bool VadIsSpeechDetected
        {
            get { return _isVadDetectingSpeech; }
        }

        private readonly WebRtcPreprocessor _preprocessor;
        #endregion

        #region construction
        public WebRtcPreprocessingPipeline([NotNull] WaveFormat inputFormat)
            : base(inputFormat, CalculateInputFrameSize(inputFormat.SampleRate), 480, 48000, 480, 48000)
        {
            _preprocessor = new WebRtcPreprocessor();
        }

        private static int CalculateInputFrameSize(int inputSampleRate)
        {
            //Take input in 20ms frames
            return (int)(inputSampleRate * 0.02);
        }
        #endregion

        public override void Dispose()
        {
            base.Dispose();

            _preprocessor.Dispose();
        }

        protected override void ApplyReset()
        {
            _preprocessor.Reset();

            base.ApplyReset();
        }

        protected override void PreprocessAudioFrame(float[] frame)
        {
            _isVadDetectingSpeech = _preprocessor.Process(WebRtcPreprocessor.SampleRates.SampleRate48KHz, frame, frame);

            SendSamplesToSubscribers(frame);
        }

        private sealed class WebRtcPreprocessor
            : IDisposable
        {
            #region native methods
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern IntPtr Dissonance_CreatePreprocessor(NoiseSuppressionLevels nsLevel);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern void Dissonance_DestroyPreprocessor(IntPtr handle);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern void Dissonance_ConfigureNoiseSuppression(IntPtr handle, NoiseSuppressionLevels nsLevel);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern bool Dissonance_GetVadSpeechState(IntPtr handle);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern ProcessorErrors Dissonance_PreprocessCaptureFrame(IntPtr handle, int sampleRate, float[] input, float[] output);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern bool Dissonance_PreprocessorExchangeInstance(IntPtr previous, IntPtr replacement);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            private static extern int Dissonance_GetFilterState();

            public enum SampleRates
            {
                // ReSharper disable UnusedMember.Local
                SampleRate8KHz = 8000,
                SampleRate16KHz = 16000,
                SampleRate32KHz = 32000,
                SampleRate48KHz = 48000,
                // ReSharper restore UnusedMember.Local
            }

            private enum ProcessorErrors
            {
                // ReSharper disable UnusedMember.Local
                Ok,

                Unspecified = -1,
                CreationFailed = -2,
                UnsupportedComponent = -3,
                UnsupportedFunction = -4,
                NullPointer = -5,
                BadParameter = -6,
                BadSampleRate = -7,
                BadDataLength = -8,
                BadNumberChannels = -9,
                FileError = -10,
                StreamParameterNotSet = -11,
                NotEnabled = -12,
                // ReSharper restore UnusedMember.Local
            }

            private enum FilterState
            {
                // ReSharper disable UnusedMember.Local
                FilterNotRunning,
                FilterNoInstance,
                FilterNoSamplesSubmitted,
                FilterOk
                // ReSharper restore UnusedMember.Local
            }
            #endregion

            #region properties and fields
            private readonly LockedValue<IntPtr> _handle;

            private readonly List<PropertyChangedEventHandler> _subscribed = new List<PropertyChangedEventHandler>();

            private NoiseSuppressionLevels _nsLevel;
            private NoiseSuppressionLevels NoiseSuppressionLevel
            {
                get { return _nsLevel; }
                set
                {
                    using (var handle = _handle.Lock())
                    {
                        _nsLevel = value;
                        if (handle.Value != IntPtr.Zero)
                            Dissonance_ConfigureNoiseSuppression(handle.Value, _nsLevel);
                    }
                }
            }
            #endregion

            public WebRtcPreprocessor()
            {
                _nsLevel = VoiceSettings.Instance.DenoiseAmount;
                _handle = new LockedValue<IntPtr>(Dissonance_CreatePreprocessor(NoiseSuppressionLevel));

                using (var handle = _handle.Lock())
                    SetFilterPreprocessor(handle.Value);
            }

            public bool Process(SampleRates inputSampleRate, float[] input, float[] output)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "5C97EF6A-353B-4B96-871F-1073746B5708");

                    var result = Dissonance_PreprocessCaptureFrame(handle.Value, (int)inputSampleRate, input, output);
                    if (result != ProcessorErrors.Ok)
                        throw Log.CreatePossibleBugException(string.Format("Preprocessor error: '{0}'", result), "0A89A5E7-F527-4856-BA01-5A19578C6D88");

                    return Dissonance_GetVadSpeechState(handle.Value);
                }
            }

            public void Reset()
            {
                using (var handle = _handle.Lock())
                {
                    Log.Debug("Resetting");

                    if (handle.Value != IntPtr.Zero)
                    {
                        //Clear from playback filter. This internally acquires a lock and will not complete until it is safe to (i.e. no one else is using the preprocessor concurrently).
                        ClearFilterPreprocessor();

                        //Destroy it
                        Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }

                    //Create a new one
                    handle.Value = Dissonance_CreatePreprocessor(NoiseSuppressionLevel);

                    //Associate with playback filter
                    SetFilterPreprocessor(handle.Value);
                }
            }

            private void SetFilterPreprocessor(IntPtr preprocessor)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "3BA66D46-A7A6-41E8-BE38-52AFE5212ACD");

                    Log.Debug("Exchanging preprocessor instance in playback filter...");

                    if (!Dissonance_PreprocessorExchangeInstance(IntPtr.Zero, handle.Value))
                        throw Log.CreatePossibleBugException("Cannot associate preprocessor with Playback filter - one already exists", "D5862DD2-B44E-4605-8D1C-29DD2C72A70C");

                    Log.Debug("...Exchanged preprocessor instance in playback filter");

                    var state = (FilterState)Dissonance_GetFilterState();
                    if (state == FilterState.FilterNotRunning)
                        Log.Warn("Associated preprocessor with playback filter - but filter is not running");

                    Bind(s => s.DenoiseAmount, "DenoiseAmount", v => NoiseSuppressionLevel = (NoiseSuppressionLevels)v);
                }
            }

            private void Bind<T>(Func<VoiceSettings, T> getValue, string propertyName, Action<T> setValue)
            {
                var settings = VoiceSettings.Instance;

                //Bind for value changes in the future
                PropertyChangedEventHandler subbed;
                settings.PropertyChanged += subbed = (sender, args) => {
                    if (args.PropertyName == propertyName)
                        setValue(getValue(settings));
                };

                //Save this subscription so we can *unsub* later
                _subscribed.Add(subbed);

                //Invoke immediately to pull the current value
                subbed.Invoke(settings, new PropertyChangedEventArgs(propertyName));
            }

            private bool ClearFilterPreprocessor(bool throwOnError = true)
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value == IntPtr.Zero)
                        throw Log.CreatePossibleBugException("Attempted  to access a null WebRtc Preprocessor encoder", "2DBC7779-F1B9-45F2-9372-3268FD8D7EBA");

                    Log.Debug("Clearing preprocessor instance in playback filter...");

                    //Clear binding in native code
                    if (!Dissonance_PreprocessorExchangeInstance(handle.Value, IntPtr.Zero))
                    {
                        if (throwOnError)
                            throw Log.CreatePossibleBugException("Cannot clear preprocessor from Playback filter", "6323106A-04BD-4217-9ECA-6FD49BF04FF0");
                        else
                            Log.Error("Failed to clear preprocessor from playback filter", "CBC6D727-BE07-4073-AA5A-F750A0CC023D");

                        return false;
                    }

                    //Clear event handlers from voice settings
                    var settings = VoiceSettings.Instance;
                    for (var i = 0; i < _subscribed.Count; i++)
                        settings.PropertyChanged -= _subscribed[i];
                    _subscribed.Clear();

                    Log.Debug("...Cleared preprocessor instance in playback filter");
                    return true;
                }
            }

            #region dispose
            private void ReleaseUnmanagedResources()
            {
                using (var handle = _handle.Lock())
                {
                    if (handle.Value != IntPtr.Zero)
                    {
                        ClearFilterPreprocessor(throwOnError: false);

                        Dissonance_DestroyPreprocessor(handle.Value);
                        handle.Value = IntPtr.Zero;
                    }
                }
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~WebRtcPreprocessor()
            {
                ReleaseUnmanagedResources();
            }
            #endregion
        }
    }
}
