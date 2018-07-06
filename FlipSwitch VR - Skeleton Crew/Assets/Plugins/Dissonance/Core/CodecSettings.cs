using Dissonance.Audio.Codecs;
using Dissonance.Audio.Codecs.Opus;
using Dissonance.Config;

namespace Dissonance
{
    internal class CodecSettings
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(CodecSettings).Name);

        private bool _started;

        private bool _settingsReady;
        private readonly object _settingsWriteLock = new object();

        private uint _decoderFrameSize;
        public uint FrameSize
        {
            get
            {
                Generate();
                return _decoderFrameSize;
            }
        }

        private int _decoderSampleRate;
        public int SampleRate
        {
            get
            {
                Generate();
                return _decoderSampleRate;
            }
        }

        private AudioQuality _encoderQuality;
        private FrameSize _encoderFrameSize;
        
        public void Start()
        {
            //Save encoder settings to ensure we use the same settings every time it is restarted
            _encoderQuality = VoiceSettings.Instance.Quality;
            _encoderFrameSize = VoiceSettings.Instance.FrameSize;
            _started = true;
        }

        private void Generate()
        {
            if (!_started)
                throw Log.CreatePossibleBugException("Attempted to use codec settings before codec settings loaded", "9D4F1C1E-9C09-424A-86F7-B633E71EF100");

            if (!_settingsReady)
            {
                lock (_settingsWriteLock)
                {
                    if (!_settingsReady)
                    {
                        //Create and destroy an encoder to determine the decoder settings to use
                        var encoder = new OpusEncoder(VoiceSettings.Instance.Quality, VoiceSettings.Instance.FrameSize);
                        _decoderFrameSize = (uint)encoder.FrameSize;
                        _decoderSampleRate = encoder.SampleRate;
                        encoder.Dispose();

                        _settingsReady = true;
                    }
                }
            }
        }

        [NotNull] public IVoiceEncoder CreateEncoder()
        {
            if (!_started)
                throw Log.CreatePossibleBugException("Attempted to use codec settings before codec settings loaded", "0BF71972-B96C-400B-B7D9-3E2AEE160470");

            return new OpusEncoder(_encoderQuality, _encoderFrameSize);
        }
    }
}
