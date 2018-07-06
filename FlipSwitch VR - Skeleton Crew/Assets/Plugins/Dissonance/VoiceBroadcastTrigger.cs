using System;
using Dissonance.Audio;
using Dissonance.VAD;
using UnityEngine;

namespace Dissonance
{
    /// <summary>
    ///     Opens and closes voice comm channels to a room or specific player in response to events
    ///     such as voice activation, push to talk, or local player proximity.
    /// </summary>
    public class VoiceBroadcastTrigger
        : BaseCommsTrigger, IVoiceActivationListener
    {
        #region field and properties
        private PlayerChannel? _playerChannel;
        private RoomChannel? _roomChannel;

        private bool _isVadSpeaking;
        private bool _scriptDeactivated;
        private CommActivationMode? _previousMode;
        private IDissonancePlayer _self;

        private Fader _activationFader = new Fader();
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField] private VolumeFaderSettings _activationFaderSettings = new VolumeFaderSettings {
            Volume = 1,
            FadeIn = TimeSpan.Zero,
            FadeOut = TimeSpan.FromSeconds(0.15f)
        };
        /// <summary>
        /// Access volume fader settings which are applied every time the trigger activates with PTT/VAD
        /// </summary>
        [NotNull] public VolumeFaderSettings ActivationFader
        {
            get { return _activationFaderSettings; }
        }

        private Fader _triggerFader = new Fader();
        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Confuses unity serialization)
        [SerializeField] private VolumeFaderSettings _triggerFaderSettings = new VolumeFaderSettings {
            Volume = 1,
            FadeIn = TimeSpan.FromSeconds(0.75f),
            FadeOut = TimeSpan.FromSeconds(1.15f)
        };
        /// <summary>
        /// Access volume fader settings which are applied every time the collider trigger is entered/exited
        /// </summary>
        [NotNull] public VolumeFaderSettings ColliderTriggerFader
        {
            get { return _triggerFaderSettings; }
        }

        private float CurrentFaderVolume
        {
            get { return _activationFader.Volume * (UseTrigger ? _triggerFader.Volume : 1); }
        }

        [SerializeField]private bool _broadcastPosition = true;
        /// <summary>
        /// Get or set if voice sent with this broadcast trigger should use positional playback
        /// </summary>
        public bool BroadcastPosition
        {
            get { return _broadcastPosition; }
            set
            {
                if (_broadcastPosition != value)
                {
                    _broadcastPosition = value;

                    if (_playerChannel.HasValue)
                    {
                        var channel = _playerChannel.Value;
                        channel.Positional = value;
                    }

                    if (_roomChannel.HasValue)
                    {
                        var channel = _roomChannel.Value;
                        channel.Positional = value;
                    }
                }
            }
        }

        [SerializeField]private CommTriggerTarget _channelType;
        /// <summary>
        /// Get or set the target type of voice sent with this trigger
        /// </summary>
        public CommTriggerTarget ChannelType
        {
            get { return _channelType; }
            set
            {
                if (_channelType != value)
                {
                    _channelType = value;

                    //Close the channel because it's type has been changed. Next update will automatically open the channel if necessary.
                    CloseChannel();
                }
            }
        }

        [SerializeField]private string _inputName;
        /// <summary>
        /// Get or set the input axis name (only applicable if this trigger is using Push-To-Talk)
        /// </summary>
        public string InputName
        {
            get { return _inputName; }
            set { _inputName = value; }
        }

        [SerializeField]private CommActivationMode _mode = CommActivationMode.VoiceActivation;
        /// <summary>
        /// Get or set how the player indicates speaking intent to this trigger
        /// </summary>
        public CommActivationMode Mode
        {
            get { return _mode; }
            set { _mode = value; }
        }

        [SerializeField]private string _playerId;
        /// <summary>
        /// Get or set the target player ID of this trigger (only applicable if the channel type is 'player')
        /// </summary>
        public string PlayerId
        {
            get { return _playerId; }
            set
            {
                if (_playerId != value)
                {
                    _playerId = value;

                    //Since the player ID has changed we need to close the channel. Next update will open it if necessary
                    if (_channelType == CommTriggerTarget.Player)
                        CloseChannel();
                }
            }
        }

        [SerializeField]private bool _useTrigger;
        /// <summary>
        /// Get or set if this broadcast trigger should use a unity trigger volume
        /// </summary>
        public override bool UseTrigger
        {
            get { return _useTrigger; }
            set { _useTrigger = value; }
        }

        [SerializeField]private string _roomName;
        /// <summary>
        /// Get or set the target room of this trigger (only applicable if the channel type is 'room')
        /// </summary>
        public string RoomName
        {
            get { return _roomName; }
            set
            {
                if (_roomName != value)
                {
                    _roomName = value;

                    //Since the room has changed we need to close the channel. Next update will open it if necessary
                    if (_channelType == CommTriggerTarget.Room)
                        CloseChannel();
                }
            }
        }

        [SerializeField]private ChannelPriority _priority = ChannelPriority.None;
        /// <summary>
        /// Get or set the priority of voice sent with this trigger
        /// </summary>
        public ChannelPriority Priority
        {
            get { return _priority; }
            set
            {
                if (_priority != value)
                {
                    _priority = value;

                    if (_playerChannel.HasValue)
                    {
                        var channel = _playerChannel.Value;
                        channel.Priority = value;
                    }

                    if (_roomChannel.HasValue)
                    {
                        var channel = _roomChannel.Value;
                        channel.Priority = value;
                    }
                }
            }
        }

        /// <summary>
        /// Get if this voice broadcast trigger is currently transmitting voice
        /// </summary>
        public bool IsTransmitting
        {
            get { return _playerChannel != null || _roomChannel != null; }
        }

        private bool _wasUserActivated;

        public override bool CanTrigger
        {
            get
            {
                // - Cannot broadcast if disabled by scripts
                if (_scriptDeactivated)
                    return false;

                // - Cannot broadcast to self if self is null
                if (_channelType == CommTriggerTarget.Self && _self == null)
                    return false;

                // - Cannot broadcast to yourself (by sibling component)!
                if (_channelType == CommTriggerTarget.Self && _self != null && _self.Type == NetworkPlayerType.Local)
                    return false;

                // - Cannot broadcast to yourself (by name)
                if (_channelType == CommTriggerTarget.Player && Comms.LocalPlayerName == _playerId)
                    return false;

                return true;
            }
        }
        #endregion

        protected override void Start()
        {
            base.Start();

            _self = GetComponent<IDissonancePlayer>();
        }

        public void OnDisable()
        {
            CloseChannel();

            _wasUserActivated = false;
        }

        public void OnDestroy()
        {
            CloseChannel();
            if (Comms != null)
                Comms.UnsubscribeFromVoiceActivation(this);
        }

        protected override void Update()
        {
            base.Update();

            //Early exit sanity check (we can't do anything useful if there's no voice comms object)
            if (!CheckVoiceComm())
                return;

            if (_previousMode != Mode)
                SwitchMode();

            //Update volume fader and apply to channels (if none are open that's fine)
            _triggerFader.Update(Time.deltaTime);
            _activationFader.Update(Time.deltaTime);
            SetChannelVolume(CurrentFaderVolume);

            //Check user intent and apply activation fading
            var intent = IsUserActivated();
            if (_wasUserActivated != intent)
            {
                if (intent)
                    _activationFader.FadeTo(_activationFaderSettings.Volume, (float)_activationFaderSettings.FadeIn.TotalSeconds);
                else
                    _activationFader.FadeTo(0, (float)_activationFaderSettings.FadeOut.TotalSeconds);
            }
            _wasUserActivated = intent;

            //Determine if we need to change state
            var current = IsTransmitting;
            var next = ShouldActivate(intent);
            if (current != next)
            {
                if (current)
                {
                    //Close the channel once the fade out is complete
                    if (CurrentFaderVolume <= 0)
                        CloseChannel();
                }
                else
                {
                    OpenChannel();
                }
            }
        }

        protected override void ColliderTriggerChanged()
        {
            base.ColliderTriggerChanged();

            //Trigger has changed state, so did we just enable or just disable?
            if (IsColliderTriggered)
                _triggerFader.FadeTo(_triggerFaderSettings.Volume, (float)_triggerFaderSettings.FadeIn.TotalSeconds);
            else
                _triggerFader.FadeTo(0, (float)_triggerFaderSettings.FadeOut.TotalSeconds);
        }

        #region manual activation
        /// <summary>
        /// Allow this broadcast trigger to transmit voice
        /// </summary>
        [Obsolete("This is equivalent to enabling this component")]    //Marked obsolete after v1.0.5 (2017-03-15)
        public void StartSpeaking()
        {
            _scriptDeactivated = false;
        }

        /// <summary>
        /// Prevent this broadcast trigger from speaking until StartSpeaking is called
        /// </summary>
        [Obsolete("This is equivalent to disabling this component")]    //Marked obsolete after v1.0.5 (2017-03-15)
        public void StopSpeaking()
        {
            _scriptDeactivated = true;
        }
        #endregion

        private void SwitchMode()
        {
            if (!CheckVoiceComm())
                return;

            CloseChannel();
            _scriptDeactivated = false;

            if (_previousMode == CommActivationMode.VoiceActivation && Mode != CommActivationMode.VoiceActivation)
            {
                Comms.UnsubscribeFromVoiceActivation(this);
                _isVadSpeaking = false;
            }

            if (Mode == CommActivationMode.VoiceActivation)
                Comms.SubcribeToVoiceActivation(this);

            _previousMode = Mode;
        }

        private bool ShouldActivate(bool intent)
        {
            //Check some situations where activating is impossible...
            if (!CanTrigger)
            {
                if (_channelType == CommTriggerTarget.Self && _self == null)
                    Log.Error("Attempting to broadcast to 'Self' but no sibling IDissonancePlayer component found");

                return false;
            }

            //Only activate if the local player has the correct tokens (and the set of required tokens for this trigger is not empty)
            intent &= TokenActivationState;

            //Only activate if the trigger is activated
            intent &= (!UseTrigger || IsColliderTriggered);

            return intent;
        }

        private bool IsUserActivated()
        {
            //Test the actual activation systems
            switch (Mode)
            {
                case CommActivationMode.VoiceActivation:
                    return _isVadSpeaking;

                case CommActivationMode.PushToTalk:
                    return Input.GetAxis(InputName) > 0.5f;

                case CommActivationMode.None:
                    return false;

                default:
                    Log.Error("Unknown Activation Mode '{0}'", Mode);
                    return false;
            }
        }

        #region channel management
        private void SetChannelVolume(float value)
        {
            if (_playerChannel.HasValue)
            {
                var c = _playerChannel.Value;
                c.Volume = value;
            }

            if (_roomChannel.HasValue)
            {
                var c = _roomChannel.Value;
                c.Volume = value;
            }
        }

        private void OpenChannel()
        {
            if (!CheckVoiceComm())
                return;

            if (ChannelType == CommTriggerTarget.Room)
            {
                _roomChannel = Comms.RoomChannels.Open(RoomName, _broadcastPosition, _priority, CurrentFaderVolume);
            }
            else if (ChannelType == CommTriggerTarget.Player)
            {
                if (PlayerId != null)
                    _playerChannel = Comms.PlayerChannels.Open(PlayerId, _broadcastPosition, _priority, CurrentFaderVolume);
                else
                    Log.Warn("Attempting to transmit to a null player ID");
            }
            else if (ChannelType == CommTriggerTarget.Self)
            {
                //Don't warn if self does not have an ID yet - it could just be initialising
                if (_self == null)
                    Log.Warn("Attempting to transmit to a null player object");
                else if (_self.PlayerId != null)
                    _playerChannel = Comms.PlayerChannels.Open(_self.PlayerId, _broadcastPosition, _priority);
            }
        }

        private void CloseChannel()
        {
            if (_roomChannel != null)
            {
                _roomChannel.Value.Dispose();
                _roomChannel = null;
            }

            if (_playerChannel != null)
            {
                _playerChannel.Value.Dispose();
                _playerChannel = null;
            }
        }
        #endregion

        #region IVoiceActivationListener impl
        void IVoiceActivationListener.VoiceActivationStart()
        {
            _isVadSpeaking = true;
        }

        void IVoiceActivationListener.VoiceActivationStop()
        {
            _isVadSpeaking = false;
        }
        #endregion
    }
}
