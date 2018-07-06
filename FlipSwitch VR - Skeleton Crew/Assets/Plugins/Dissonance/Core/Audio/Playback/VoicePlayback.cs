using System;
using Dissonance.Audio.Codecs;
using Dissonance.Networking;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Handles decoding and playing audio for a specific remote player.
    ///     Entities with this behaviour are created automatically by the DissonanceVoiceComms component.
    /// </summary>
    public class VoicePlayback
        : MonoBehaviour, IVolumeProvider
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, "Voice Playback Component");

        private readonly SpeechSessionStream _sessions;

        // ReSharper disable once MemberCanBePrivate.Global (Justificiation: Public API)
        public AudioSource AudioSource { get; private set; }
        public bool PositionTrackingAvailable { get; internal set; }

        private SamplePlaybackComponent _player;
        private WaveFormat _inputFormat;
        private uint _inputFrameSize;
        private float? _savedSpatialBlend;

        /// <summary>
        /// Get the name of the player speaking through this component
        /// </summary>
        public string PlayerName
        {
            get { return _sessions.PlayerName; }
            internal set { _sessions.PlayerName = value; }
        }

        /// <summary>
        /// Get a value indicating if this component is currently playing audio
        /// </summary>
        public bool IsSpeaking
        {
            get { return _player != null && _player.HasActiveSession; }
        }

        /// <summary>
        /// Get the current amplitude of audio being played through this component
        /// </summary>
        public float Amplitude
        {
            get { return _player == null ? 0 : _player.ARV; }
        }

        /// <summary>
        /// Get the current priority of audio being played through this component
        /// </summary>
        public ChannelPriority Priority
        {
            get
            {
                if (_player == null)
                    return ChannelPriority.None;

                var session = _player.Session;
                return session.HasValue ? session.Value.Priority : ChannelPriority.None;
            }
        }

        /// <summary>
        /// Get or set whether this voice playback object is muted
        /// </summary>
        internal bool IsMuted { get; set; }

        /// <summary>
        /// Get or set the volume this audio should play back at
        /// </summary>
        internal float PlaybackVolume { get; set; }

        /// <summary>
        /// Get a value indicating if the playback component is doing basic spatialization itself (incompatible with other spatializers such as the oculus spatializer)
        /// </summary>
        internal bool ApplyingAudioSpatialization { get; private set; }

        internal IPriorityManager PriorityManager { get; set; }

        internal float? PacketLoss
        {
            get
            {
                var s = _player.Session;
                if (!s.HasValue)
                    return null;

                return s.Value.PacketLoss;
            }
        }

        internal float Jitter { get { return ((IJitterEstimator)_sessions).Jitter; } }
        internal float JitterConfidence { get { return ((IJitterEstimator)_sessions).Confidence; } }
        #endregion

        public VoicePlayback()
        {
            _sessions = new SpeechSessionStream(this);

            PlaybackVolume = 1;
        }

        public void Awake()
        {
            AudioSource = GetComponent<AudioSource>();
            _player = GetComponent<SamplePlaybackComponent>();

            Reset();
        }

        internal void Reset()
        {
            IsMuted = false;
            PlaybackVolume = 1;
        }

        public void OnEnable()
        {
            AudioSource.Stop();

            if (AudioSource.spatialize)
            {
                //The audio source is spatialized by something else. Simply play the audio back and let the spatializer handle everything
                ApplyingAudioSpatialization = false;
                AudioSource.clip = null;

                //Set the player not to multiply (it will simply overwrite the source audio)
                _player.MultiplyBySource = false;
            }
            else
            {
                //Nothing is spatializing the audio from this AudioSource. Play back a flatline of 1.0 and then multiple the voice signal by that (to achieve basic spatialization)
                ApplyingAudioSpatialization = true;
                AudioSource.clip = AudioClip.Create("Flatline", 4096, 1, AudioSettings.outputSampleRate, true, (buf) => {
                    for (var i = 0; i < buf.Length; i++)
                        buf[i] = 1.0f;
                });

                //Set the player to multiply by the source audio
                _player.MultiplyBySource = true;
            }

            AudioSource.Play();
        }

        public void OnDisable()
        {
            _sessions.StopSession(false);
        }

        public void Update()
        {
            if (!_player.HasActiveSession)
            {
                //We're not playing anything, so play the next session (if there is one ready)
                var s = _sessions.TryDequeueSession();
                if (s.HasValue)
                    _player.Play(s.Value);
            }
            else
            {
                //We're playing something, adjust playback speed according to the player
                AudioSource.pitch = _player.CorrectedPlaybackSpeed;
            }

            //Enable or disable positional playback depending upon if it's avilable for this speaker
            UpdatePositionalPlayback();
        }

        private void UpdatePositionalPlayback()
        {
            var session = _player.Session;
            if (session.HasValue)
            {
                if (PositionTrackingAvailable && session.Value.Positional)
                {
                    if (_savedSpatialBlend.HasValue)
                    {
                        Log.Debug("Changing to positional playback for {0}", PlayerName);
                        AudioSource.spatialBlend = _savedSpatialBlend.Value;
                        _savedSpatialBlend = null;
                    }
                }
                else
                {
                    if (!_savedSpatialBlend.HasValue)
                    {
                        Log.Debug("Changing to non-positional playback for {0}", PlayerName);
                        _savedSpatialBlend = AudioSource.spatialBlend;
                        AudioSource.spatialBlend = 0;
                    }
                }
            }
        }

        internal void SetFormat([NotNull] WaveFormat inputFormat, uint inputFrameSize)
        {
            if (inputFormat == null)
                throw new ArgumentNullException("inputFormat", "Cannot set input wave format to be null");

            _inputFormat = inputFormat;
            _inputFrameSize = inputFrameSize;
        }

        internal void StartPlayback()
        {
            _sessions.StartSession(new FrameFormat(Codec.Opus, _inputFormat, _inputFrameSize));
        }

        internal void StopPlayback()
        {
            _sessions.StopSession();
        }

        internal void ReceiveAudioPacket(VoicePacket packet)
        {
            _sessions.ReceiveFrame(packet);
        }

        float IVolumeProvider.TargetVolume
        {
            get
            {
                //Mute if explicitly muted
                if (IsMuted)
                    return 0;

                //Mute if the top priority is greater than this priority
                if (PriorityManager != null && PriorityManager.TopPriority > Priority)
                    return 0;

                //No volume adjustments applied, so play at chosen volume
                return PlaybackVolume;
            }
        }
    }
}