using System;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    /// The state of a player in a Dissonance session
    /// </summary>
    public abstract class VoicePlayerState
    {
        private readonly string _name;

        /// <summary>
        /// Event which will be invoked whenever this player starts speaking
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnStartedSpeaking;

        /// <summary>
        /// Event which will be invoked whenever this player stops speaking
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnStoppedSpeaking;

        /// <summary>
        /// Event which will be invoked when this player leaves the session
        /// </summary>
        // ReSharper disable once EventNeverSubscribedTo.Global (Justificiation: Public API)
        public event Action<VoicePlayerState> OnLeftSession;

        #region constructor
        internal VoicePlayerState(string name)
        {
            _name = name;
        }
        #endregion

        #region properties
        /// <summary>
        /// Get the name of the player this object represents
        /// </summary>
        [NotNull] public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Get a value indicating if this player is connected to the session
        /// </summary>
        public abstract bool IsConnected { get; }

        /// <summary>
        /// Get a value indicating if this player is currently speaking
        /// </summary>
        public abstract bool IsSpeaking { get; }

        /// <summary>
        /// The current amplitude of the voice signal from this player
        /// </summary>
        public abstract float Amplitude { get; }

        /// <summary>
        /// Get or set the volume which voice from this player should be played at
        /// </summary>
        public abstract float Volume { get; set; }

        /// <summary>
        /// Get or set whether audio from this player is muted for the local player
        /// </summary>
        public abstract bool IsLocallyMuted { get; set; }

        /// <summary>
        /// Get the voice playback instance for this player (may be null if this player does not currently have a voice playback instance assigned)
        /// </summary>
        [CanBeNull] public abstract VoicePlayback Playback { get; }

        /// <summary>
        /// Get the dissonance tracker associated with this player
        /// </summary>
        [CanBeNull] public abstract IDissonancePlayer Tracker { get; internal set; }

        internal abstract float? PacketLoss { get; }
        #endregion

        #region event invokers
        internal void InvokeOnStoppedSpeaking()
        {
            if (Playback != null)
                Playback.StopPlayback();

            var evt = OnStoppedSpeaking;
            if (evt != null)
                evt(this);
        }

        internal void InvokeOnStartedSpeaking()
        {
            if (Playback != null)
                Playback.StartPlayback();

            var evt = OnStartedSpeaking;
            if (evt != null)
                evt(this);
        }

        internal void InvokeOnLeftSession()
        {
            var evt = OnLeftSession;
            if (evt != null)
                evt(this);
        }
        #endregion

        internal abstract void Update();
    }

    internal class LocalVoicePlayerState
        : VoicePlayerState
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(LocalVoicePlayerState).Name);

        private readonly IMicrophoneProvider _micProvider;
        private readonly RoomChannels _roomChannels;
        private readonly PlayerChannels _playerChannels;

        public LocalVoicePlayerState(string name, IMicrophoneProvider micProvider, RoomChannels roomChannels, PlayerChannels playerChannels)
            : base(name)
        {
            _micProvider = micProvider;
            _roomChannels = roomChannels;
            _playerChannels = playerChannels;

            roomChannels.OpenedChannel += OnChannelOpened;
            roomChannels.ClosedChannel += OnChannelClosed;
            playerChannels.OpenedChannel += OnChannelOpened;
            playerChannels.ClosedChannel += OnChannelClosed;
        }

        private void OnChannelOpened(string channel, ChannelProperties properties)
        {
            var count = _playerChannels.Count + _roomChannels.Count;
            if (count == 1)
            {
                Log.Debug("Local player started speaking");
                InvokeOnStartedSpeaking();
            }
        }

        private void OnChannelClosed(string channel, ChannelProperties properties)
        {
            var count = _playerChannels.Count + _roomChannels.Count;
            if (count == 0)
            {
                Log.Debug("Local player stopped speaking");
                InvokeOnStoppedSpeaking();
            }
        }

        public override bool IsConnected
        {
            get { return true; }
        }

        public override VoicePlayback Playback
        {
            get { return null; }
        }

        public override bool IsLocallyMuted
        {
            get
            {
                //Local microphone audio is never played through the local speakers - i.e. the local player is always locally muted
                return true;
            }
            set
            {
                if (!value)
                {
                    Log.Error(Log.UserErrorMessage(
                        "Attempted to Locally UnMute the local player",
                        "Setting `IsLocallyMuted = false` on the local player",
                        "https://dissonance.readthedocs.io/en/latest/Reference/Other/VoicePlayerState/",
                        "BEF78918-1805-4D59-A071-74E7B38D13C8"
                    ));
                }
            }
        }

        public override IDissonancePlayer Tracker { get; internal set; }

        public override float Amplitude
        {
            get
            {
                var mic = _micProvider.MicCapture;
                return mic == null ? 0 : mic.Amplitude;
            }
        }
        
        public override float Volume
        {
            get { return 1; }

            // ReSharper disable once ValueParameterNotUsed (Justification this property isn't supported)
            set
            {
                Log.Error(Log.UserErrorMessage(
                    "Attempted to set playback volume of local player",
                    "Setting `Volume = value` on the local player",
                    "https://dissonance.readthedocs.io/en/latest/Reference/Other/VoicePlayerState/",
                    "9822EFB8-1A4A-4F54-9A32-5F183AE8D4DE"
                ));
            }
        }

        public override bool IsSpeaking
        {
            get { return _roomChannels.Count > 0 || _playerChannels.Count > 0; }
        }

        internal override float? PacketLoss { get { return null; } }

        internal override void Update()
        {
        }
    }

    internal class RemoteVoicePlayerState
        : VoicePlayerState
    {
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(RemoteVoicePlayerState).Name);

        private readonly VoicePlayback _playback;
        private IDissonancePlayer _player;

        internal RemoteVoicePlayerState(VoicePlayback playback)
            : base(playback.PlayerName)
        {
            _playback = playback;

            _playback.Reset();
        }

        /// <summary>
        /// Get a value indicating if this player is connected to the game
        /// </summary>
        public override bool IsConnected
        {
            get
            {
                //We're checking three things here:
                // 1. If playback is null something is wrong, we're going to take that to mean the player isn't connected
                // 2. If playback is inactive then this player has disconnected
                // 3. If playback has a different name it's been reassigned to another player (and one must have disconnected)
                return _playback.isActiveAndEnabled && _playback.PlayerName == Name;
            }
        }

        /// <summary>
        /// Get a value indicating if this player is speaking
        /// </summary>
        public override bool IsSpeaking
        {
            get { return IsConnected && _playback.IsSpeaking; }
        }

        /// <summary>
        /// The current amplitude of the voice signal from this player
        /// </summary>
        public override float Amplitude
        {
            get { return IsConnected ? _playback.Amplitude : 0; }
        }

        private float _volume;

        public override float Volume
        {
            get { return _volume; }
            set { _volume = value;

                var p = Playback;
                if (p)
                    p.PlaybackVolume = _volume;
            }
        }

        /// <summary>
        /// Get the voice playback instance for this player (may be null if this player does not currently have a voice playback instance assigned)
        /// </summary>
        public override VoicePlayback Playback
        {
            get { return IsConnected ? _playback : null; }
        }

        /// <summary>
        /// Get or set whether voice from this player is prevented from playing on the local machine
        /// </summary>
        public override bool IsLocallyMuted
        {
            get { return IsConnected && _playback.IsMuted; }
            set
            {
                var p = Playback;

                if (!IsConnected || p == null)
                    Log.Warn("Attempted to (un)mute player {0}, but they are not connected", Name);
                else
                    p.IsMuted = value;
            }
        }

        /// <summary>
        /// Get the dissonance tracker associated with this player
        /// </summary>
        public override IDissonancePlayer Tracker
        {
            get { return _player; }
            internal set
            {
                _player = value;

                if (_playback.PlayerName == Name)
                {
                    _playback.PositionTrackingAvailable = value != null;

                    if (!_playback.PositionTrackingAvailable)
                    {
                        _playback.transform.position = Vector3.zero;
                        _playback.transform.rotation = Quaternion.identity;
                    }
                }
            }
        }

        internal override float? PacketLoss
        {
            get
            {
                var p = Playback;
                return p ? p.PacketLoss : null;
            }
        }

        internal override void Update()
        {
            if (Tracker != null && Playback != null && Tracker.IsTracking)
            {
                Playback.transform.position = Tracker.Position;
                Playback.transform.rotation = Tracker.Rotation;
            }
        }
    }
}
