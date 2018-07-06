using System;
using System.Runtime.InteropServices;
using Dissonance.Extensions;

namespace Dissonance.VAD
{
    /// <summary>
    /// Detect voice activity on a microphone signal
    /// </summary>
    internal sealed class WebRtcVoiceDetector
        : IVoiceDetector
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(WebRtcVoiceDetector).Name);

        private WebRtcVadNative.VAD _vad;

        public bool IsSpeaking { get; private set; }
        #endregion

        #region constructor
        public WebRtcVoiceDetector(uint frameSize, int sampleRate)
        {
            _vad = new WebRtcVadNative.VAD(frameSize, sampleRate);
        }
        #endregion

        /// <summary>
        /// Clear internal state of VAD
        /// </summary>
        public void Reset()
        {
            _vad.Reset();
        }

        public void Handle(ArraySegment<short> buffer)
        {
            var previousState = IsSpeaking;

            IsSpeaking = _vad.Process(buffer);

            if (IsSpeaking != previousState)
                Log.Trace("VAD State changed to: {0}", IsSpeaking);
        }

        public void Dispose()
        {
            if (_vad != null)
            {
                _vad.Dispose();
                _vad = null;
            }
        }

        public static int? ChooseSampleRate(int minSampleRate, int maxSampleRate)
        {
            //WebRTC only allows a certain range of sample rates, choose one of:
            // - 8,000
            // - 16,000
            // - 32,000
            // - 48,000 ???

            //If both values are zero that means any value is acceptable
            //In which case choose the highest value WebRTC allows
            if (minSampleRate == 0 && maxSampleRate == 0)
                return 48000;

            if (minSampleRate <= 48000 && maxSampleRate >= 48000)
                return 48000;

            if (minSampleRate <= 32000 && maxSampleRate >= 32000)
                return 32000;

            if (minSampleRate <= 16000 && maxSampleRate >= 16000)
                return 16000;

            if (minSampleRate <= 8000 && maxSampleRate >= 8000)
                return 8000;

            //No valid sample rate, the external pipeline will have to resample the rate
            return null;
        }
    }

    internal static class WebRtcVadNative
    {
        private static class WebRtcVadNativeMethods
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern IntPtr Dissonance_WebRtcVad_Create();

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern void Dissonance_WebRtcVad_Free(IntPtr handle);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int Dissonance_WebRtcVad_Init(IntPtr handle);


#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int Dissonance_WebRtcVad_set_mode(IntPtr handle, Aggressiveness mode);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int Dissonance_WebRtcVad_Process(IntPtr handle, int fs, IntPtr audioFrame, UIntPtr frameLength);

#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
#else
            [DllImport("AudioPluginDissonance", CallingConvention = CallingConvention.Cdecl)]
#endif
            internal static extern int Dissonance_WebRtcVad_ValidRateAndFrameLength(int rate, UIntPtr frameLength);
        }

        internal enum Aggressiveness
        {
            Normal = 0,
            LowBitrate = 1,
            Aggressive = 2,
            VeryAggressive = 3
        };

        internal class WebRtcVadException
            : DissonanceException
        {
            internal WebRtcVadException(string message)
                : base(message)
            {
            }
        }

        // ReSharper disable once InconsistentNaming (Justification: TLA)
        internal class VAD
            : IDisposable
        {
            #region fields and properties
            private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof(VAD).Name);

            private readonly uint _frameSize;
            private readonly int _sampleRate;

            private IntPtr _handle;

            private Aggressiveness _mode;
            public Aggressiveness Mode
            {
                get { return _mode; }
                set
                {
                    _mode = value;
                    if (WebRtcVadNativeMethods.Dissonance_WebRtcVad_set_mode(_handle, value) != 0)
                        throw new WebRtcVadException(Log.PossibleBugMessage("Failed to set Mode on the WebRTC VAD", "D4883893-3077-43C9-9FE6-7B3101A2AA68"));
                }
            }
            #endregion

            #region constructors
            public VAD(uint frameSize, int sampleRate, Aggressiveness mode = Aggressiveness.Normal)
            {
                _frameSize = frameSize;
                _sampleRate = sampleRate;

                if (!IsValidUsage(sampleRate, frameSize))
                    throw new WebRtcVadException(Log.PossibleBugMessage("Failed to initialize WebRTC VAD (incorrect frame size or sample rate)", "E74CF7D3-51C0-4240-B552-02EB58EAE34D"));

                //Create handle
                _handle = WebRtcVadNativeMethods.Dissonance_WebRtcVad_Create();
                if (WebRtcVadNativeMethods.Dissonance_WebRtcVad_Init(_handle) != 0)
                    throw new WebRtcVadException(Log.PossibleBugMessage("Failed to initialize WebRTC VAD", "87596AD8-9096-4FEB-867D-B23A1A7F7F91"));

                //Initialize defaults
                Mode = mode;
            }
            #endregion

            #region disposal
            ~VAD()
            {
                Dispose();
            }

            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                GC.SuppressFinalize(this);

                if (_handle != IntPtr.Zero)
                {
                    WebRtcVadNativeMethods.Dissonance_WebRtcVad_Free(_handle);
                    _handle = IntPtr.Zero;
                }

                _disposed = true;
            }
            #endregion

            public void Reset()
            {
                //Init a new VAD state in the existing space
                if (WebRtcVadNativeMethods.Dissonance_WebRtcVad_Init(_handle) != 0)
                    throw new WebRtcVadException(Log.PossibleBugMessage("Failed to re-initialize WebRTC VAD", "4A2E754F-5031-476C-A6FA-C0F883832806"));
            }

            public bool Process(ArraySegment<short> frame)
            {
                if (frame.Count != _frameSize)
                    throw Log.CreatePossibleBugException(string.Format("Frame is incorrect size (expected {0} for {1})", _frameSize, frame.Count), "716C8CBD-8014-4C57-889C-0450ECF96A0F");

                using (var pin = frame.Pin())
                {
                    var result = WebRtcVadNativeMethods.Dissonance_WebRtcVad_Process(_handle, _sampleRate, pin.Ptr, new UIntPtr((uint)frame.Count));

                    if (result == -1)
                        throw Log.CreatePossibleBugException("Unknown error processing audio", "9BBD8BB1-B08D-4F34-AFF2-AAF32F69C309");
                    return result == 1;
                }
            }

            #region static helpers
            private static bool IsValidUsage(int sampleRate, uint frameLength)
            {
                return WebRtcVadNativeMethods.Dissonance_WebRtcVad_ValidRateAndFrameLength(sampleRate, new UIntPtr(frameLength)) == 0;
            }
            #endregion
        }
    }
}