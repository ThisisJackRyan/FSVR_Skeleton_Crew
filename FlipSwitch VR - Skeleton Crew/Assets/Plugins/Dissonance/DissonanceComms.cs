using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using Dissonance.Config;
using Dissonance.Networking;
using Dissonance.VAD;
using UnityEngine;

#pragma warning disable 0618

namespace Dissonance
{
    /// <summary>
    ///     The central Dissonance Voice Comms component.
    ///     Place one of these on a voice comm entity near the root of your scene.
    /// </summary>
    /// <remarks>
    ///     Handles recording the local player's microphone and sending the data to the network.
    ///     Handles managing the playback entities for the other users on the network.
    ///     Provides the API for opening and closing channels.
    /// </remarks>
    public sealed class DissonanceComms
        : MonoBehaviour, IPriorityManager, IAccessTokenCollection, IChannelPriorityProvider, IMicrophoneProvider
    {
        #region fields
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(DissonanceComms).Name);

        private bool _started;

        private readonly Rooms _rooms = new Rooms();
        private readonly PlayerChannels _playerChannels;
        private readonly RoomChannels _roomChannels;
        private readonly TextChat _text;

        private readonly PlayerTrackerManager _playerTrackers;
        private readonly PlaybackPool _playbackPool;
        private readonly PlayerCollection _players = new PlayerCollection();
        private readonly CodecSettings _codecSettings = new CodecSettings();
        private readonly PriorityManager _playbackPriorityManager;
        private readonly CapturePipelineManager _capture;

        private ICommsNetwork _net;
        private string _localPlayerName;

        [SerializeField]private bool _isMuted;
        [SerializeField]private VoicePlayback _playbackPrefab;
        [SerializeField]private string _micName;
        [SerializeField]private ChannelPriority _playerPriority = ChannelPriority.Default;
        [SerializeField]private TokenSet _tokens = new TokenSet();

        // ReSharper disable EventNeverSubscribedTo.Global (Justification: Part of public API)
        public event Action<VoicePlayerState> OnPlayerJoinedSession;
        public event Action<VoicePlayerState> OnPlayerLeftSession;
        public event Action<VoicePlayerState> OnPlayerStartedSpeaking;
        public event Action<VoicePlayerState> OnPlayerStoppedSpeaking;
        public event Action<string> LocalPlayerNameChanged;
        // ReSharper restore EventNeverSubscribedTo.Global
        #endregion

        public DissonanceComms()
        {
            _playbackPool = new PlaybackPool(_codecSettings, (IPriorityManager)this);
            _playerChannels = new PlayerChannels((IChannelPriorityProvider)this);
            _roomChannels = new RoomChannels((IChannelPriorityProvider)this);
            _text = new TextChat(() => _net);
            _playerTrackers = new PlayerTrackerManager(_players);
            _playbackPriorityManager = new PriorityManager(_players);
            _capture = new CapturePipelineManager(_codecSettings, _roomChannels, _playerChannels, Players) {
                MicrophoneName = _micName
            };
        }

        #region properties
        internal float PacketLoss
        {
            get { return _capture.PacketLoss; }
        }

        /// <summary>
        /// Get or set the local player name (may only be set before this component starts)
        /// </summary>
        public string LocalPlayerName
        {
            get { return _localPlayerName; }
            set
            {
                if (_localPlayerName == value)
                    return;

                if (_started)
                    throw Log.CreateUserErrorException("Cannot set player name when the component has been started", "directly setting the 'LocalPlayerName' property too late", "https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms.md", "58973EDF-42B5-4FF1-BE01-FFF28300A97E");

                _localPlayerName = value;

                var handler = LocalPlayerNameChanged;
                if (handler != null) handler(value);
            }
        }

        /// <summary>
        /// Get a value indicating if Dissonance has successfully connected to a voice network yet
        /// </summary>
        public bool IsNetworkInitialized
        {
            get { return _net.Status == ConnectionStatus.Connected; }
        }
        
        /// <summary>
        /// Get an object to control which rooms the local player is listening to
        /// </summary>
        [NotNull] public Rooms Rooms
        {
            get { return _rooms; }
        }

        /// <summary>
        /// Get an object to control channels to other players
        /// </summary>
        [NotNull] public PlayerChannels PlayerChannels
        {
            get { return _playerChannels; }
        }

        /// <summary>
        /// Get an object to control channels to rooms (transmitting)
        /// </summary>
        [NotNull] public RoomChannels RoomChannels
        {
            get { return _roomChannels; }
        }

        /// <summary>
        /// Get an object to send and receive text messages
        /// </summary>
        [NotNull] public TextChat Text
        {
            get { return _text; }
        }

        /// <summary>
        /// Get a list of states of all players in the Dissonance voice session
        /// </summary>
        [NotNull] public ReadOnlyCollection<VoicePlayerState> Players
        {
            get { return _players.Readonly; }
        }

        /// <summary>
        /// Get the priority of the current highest priority speaker
        /// </summary>
        public ChannelPriority TopPrioritySpeaker
        {
            get { return _playbackPriorityManager.TopPriority; }
        }

        /// <summary>
        /// Get the set of tokens the local player has knowledge of
        /// </summary>
        [NotNull] public IEnumerable<string> Tokens
        {
            get { return _tokens; }
        }

        /// <summary>
        /// The default priority to use for this player if a broadcast trigger does not specify a priority
        /// </summary>
        public ChannelPriority PlayerPriority
        {
            get { return _playerPriority; }
            set { _playerPriority = value; }
        }

        /// <summary>
        /// Get or set the microphone device name to use for voice capture (may only be set before this component Starts)
        /// </summary>
        [CanBeNull] public string MicrophoneName
        {
            get { return _micName; }
            set
            {
                if (_micName == value)
                    return;

                if (_started)
                    Log.Info("Changing microphone device from '{0}' to '{1}'", _micName, value);

                _capture.MicrophoneName = value;
                _micName = value;
            }
        }

        /// <summary>
        /// Get or set the prefab to use for voice playback (may only be set before this component Starts)
        /// </summary>
        public VoicePlayback PlaybackPrefab
        {
            get { return _playbackPrefab; }
            set
            {
                if (_started)
                    throw Log.CreateUserErrorException("Cannot set playback prefab when the component has been started", "directly setting the 'PlaybackPrefab' property too late", "https://placeholder-software.co.uk/dissonance/docs/Reference/Components/Dissonance-Comms.md", "A0796DA8-A0BC-49E4-A1B3-F0AA0F51BAA0");

                _playbackPrefab = value;
            }
        }

        /// <summary>
        /// Get or set if the local player is muted (prevented from sending any voice transmissions)
        /// </summary>
        public bool IsMuted
        {
            get { return _isMuted; }
            set { _isMuted = value; }
        }
        #endregion

        private void Start()
        {
            //Ensure that all settings are loaded before we access them (potentially from other threads)
            DebugSettings.Preload();
            VoiceSettings.Preload();

            //Write multithreaded logs ASAP so the logging system knows which is the main thread
            Logs.WriteMultithreadedLogs();

            //Sanity check (can't run without a network object)
            var net = gameObject.GetComponent<ICommsNetwork>();
            if (net == null)
                throw new Exception("Cannot find a voice network component. Please attach a voice network component appropriate to your network system to the DissonanceVoiceComms' entity.");

            //Sanity check (can't run without run in background). This value doesn't work on mobile platforms so don't perform this check there
            if (!Application.isMobilePlatform && !Application.runInBackground)
                Log.Error(Log.UserErrorMessage("Run In Background is not set", "The 'Run In Background' toggle on the player settings has not been checked", "https://dissonance.readthedocs.io/en/latest/Basics/Getting-Started/#3-run-in-background", "98D123BB-CF4F-4B41-8555-41CD01108DA7"));

            if (PlaybackPrefab == null)
            {
                Log.Info("Loading default playback prefab");
                PlaybackPrefab = Resources.Load<GameObject>("PlaybackPrefab").GetComponent<VoicePlayback>();
            }

            net.PlayerJoined += Net_PlayerJoined;
            net.PlayerLeft += Net_PlayerLeft;
            net.VoicePacketReceived += Net_VoicePacketReceived;
            net.PlayerStartedSpeaking += Net_PlayerStartedSpeaking;
            net.PlayerStoppedSpeaking += Net_PlayerStoppedSpeaking;
            net.TextPacketReceived += _text.OnMessageReceived;

            //If an explicit name has not been set generate a GUID based name
            if (string.IsNullOrEmpty(LocalPlayerName))
            {
                var guid = Guid.NewGuid().ToString();
                LocalPlayerName = guid;
            }

            //mark this component as started, locking the LocalPlayerName, PlaybackPrefab and Microphone properties from changing
            _started = true;

            //Setup the playback pool so we can create pipelines to play audio
            _playbackPool.Start(PlaybackPrefab, transform);

            //Make sure we load up the codec settings so we can create codecs later
            _codecSettings.Start();

            //Start the player collection (to set local name)
            _players.Start(LocalPlayerName, (IMicrophoneProvider)this, RoomChannels, PlayerChannels);

            net.Initialize(LocalPlayerName, Rooms, PlayerChannels, RoomChannels);
            _net = net;

            //Begin capture manager, this will create and destroy capture pipelines as necessary (net mode changes, mic name changes, mic requires reset etc)
            _capture.Start(_net);
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playmodeStateChanged += OnEditorPlaymodeChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playmodeStateChanged -= OnEditorPlaymodeChanged;
#endif
        }

        private void OnEditorPlaymodeChanged()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPaused)
                _capture.Pause();
            else if (UnityEditor.EditorApplication.isPlaying)
                _capture.Resume();
#endif
        }

        #region network events
        private void Net_PlayerStoppedSpeaking([NotNull] string player)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (player == null)

            {
                Log.Warn(Log.PossibleBugMessage("Received a player-stopped-speaking event for a null player ID", "5A424BF0-D384-4A63-B6E2-042A1F31A085"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            VoicePlayerState state;
            if (_players.TryGet(player, out state))
            {
                state.InvokeOnStoppedSpeaking();

                if (OnPlayerStoppedSpeaking != null)
                    OnPlayerStoppedSpeaking(state);
            }
        }

        private void Net_PlayerStartedSpeaking([NotNull] string player)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (player == null)
            {
                Log.Warn(Log.PossibleBugMessage("Received a player-started-speaking event for a null player ID", "CA95E783-CA35-441B-9B8B-FAA0FA0B41E3"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            VoicePlayerState state;
            if (_players.TryGet(player, out state))
            {
                state.InvokeOnStartedSpeaking();

                if (OnPlayerStartedSpeaking != null)
                    OnPlayerStartedSpeaking(state);
            }
        }

        private void Net_VoicePacketReceived(VoicePacket packet)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (packet.SenderPlayerId == null)
            {
                Log.Warn(Log.PossibleBugMessage("Received a voice packet with a null player ID (discarding)", "C0FE4E98-3CC9-466E-AA39-51F0B6D22D09"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            VoicePlayerState state;
            if (_players.TryGet(packet.SenderPlayerId, out state) && state.Playback != null)
                state.Playback.ReceiveAudioPacket(packet);
        }

        private void Net_PlayerLeft([NotNull] string playerId)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (playerId == null)
            {
                Log.Warn(Log.PossibleBugMessage("Received a player-left event for a null player ID", "37A2506B-6489-4679-BD72-1C53D69797B1"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            var state = _players.Remove(playerId);
            if (state != null)
            {
                var playback = state.Playback;
                if (playback != null)
                    _playbackPool.Put(playback);

                _playerTrackers.RemovePlayer(state);

                state.InvokeOnLeftSession();
                if (OnPlayerLeftSession != null)
                    OnPlayerLeftSession(state);
            }
        }

        private void Net_PlayerJoined([NotNull] string playerId)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode (Justification: Sanity check against network system returning incorrect values)
            if (playerId == null)
            {
                Log.Warn(Log.PossibleBugMessage("Received a player-joined event for a null player ID", "86074592-4BAD-4DF5-9B2C-1DF42A68FAF8"));
                return;
            }
            // ReSharper restore HeuristicUnreachableCode

            if (playerId == LocalPlayerName)
                return;

            //Get a playback component for this player
            var playback = _playbackPool.Get(playerId);

            //Create the state object for this player
            var state = new RemoteVoicePlayerState(playback);
            _players.Add(state);

            //Associate state with the position tracker for this player (if there is one)
            _playerTrackers.AddPlayer(state);

            //Now we've set everything up activate the playback
            playback.gameObject.SetActive(true);

            if (OnPlayerJoinedSession != null)
                OnPlayerJoinedSession(state);
        }
        #endregion

        /// <summary>
        /// Find the player state for a given player ID (or null, if it cannot be found)
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        [CanBeNull] public VoicePlayerState FindPlayer([NotNull] string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");

            VoicePlayerState state;
            if (_players.TryGet(playerId, out state))
                return state;

            return null;
        }

        private void Update()
        {
            Logs.WriteMultithreadedLogs();

            _playbackPriorityManager.Update();
            _players.Update();
            _capture.Update(IsMuted, Time.deltaTime);
        }

        private void OnDestroy()
        {
            _capture.Destroy();
        }

        #region VAD
        /// <summary>
        ///     Subscribes to automatic voice detection.
        /// </summary>
        /// <param name="listener">
        ///     The listener which is to receive notification when the player starts and stops speaking via
        ///     automatic voice detection.
        /// </param>
        public void SubcribeToVoiceActivation([NotNull] IVoiceActivationListener listener)
        {
            _capture.Subscribe(listener);
        }

        /// <summary>
        ///     Unsubsribes from automatic voice detection.
        /// </summary>
        /// <param name="listener"></param>
        public void UnsubscribeFromVoiceActivation([NotNull] IVoiceActivationListener listener)
        {
            _capture.Unsubscribe(listener);
        }
        #endregion

        #region player tracking
        /// <summary>
        /// Enable position tracking for the player represented by the given object
        /// </summary>
        /// <param name="player"></param>
        public void TrackPlayerPosition([NotNull] IDissonancePlayer player)
        {
            _playerTrackers.AddTracker(player);
        }

        /// <summary>
        /// Stop position tracking for the player represented by the given object
        /// </summary>
        /// <param name="player"></param>
        public void StopTracking([NotNull] IDissonancePlayer player)
        {
            _playerTrackers.RemoveTracker(player);
        }
        #endregion

        #region tokens
        /// <summary>
        /// Event invoked whenever a new token is added to the local set
        /// </summary>
        public event Action<string> TokenAdded
        {
            add { _tokens.TokenAdded += value; }
            remove { _tokens.TokenAdded += value; }
        }

        /// <summary>
        /// Event invoked whenever a new token is removed from the local set
        /// </summary>
        public event Action<string> TokenRemoved
        {
            add { _tokens.TokenRemoved += value; }
            remove { _tokens.TokenRemoved += value; }
        }

        /// <summary>
        /// Add the given token to the local player
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool AddToken([NotNull] string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot add a null token");

            return _tokens.AddToken(token);
        }

        /// <summary>
        /// Removed the given token from the local player
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool RemoveToken([NotNull] string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot remove a null token");

            return _tokens.RemoveToken(token);
        }

        /// <summary>
        /// Test if the local player knows the given token
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public bool ContainsToken([NotNull] string token)
        {
            if (token == null)
                throw new ArgumentNullException("token", "Cannot search for a null token");

            return _tokens.ContainsToken(token);
        }

        /// <summary>
        /// Tests if the local player knows has knowledge of *any* of the tokens in the given set
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public bool HasAnyToken([NotNull] TokenSet tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens", "Cannot intersect with a null set");

            return _tokens.IntersectsWith(tokens);
        }
        #endregion

        #region IPriorityManager explicit impl
        ChannelPriority IPriorityManager.TopPriority
        {
            get { return _playbackPriorityManager.TopPriority; }
        }
        #endregion

        #region  IChannelPriorityProvider explicit impl
        ChannelPriority IChannelPriorityProvider.DefaultChannelPriority
        {
            get { return _playerPriority; }
            set { _playerPriority = value; }
        }
        #endregion

        #region IMicrophoneProvider explicit impl
        [CanBeNull] MicrophoneCapture IMicrophoneProvider.MicCapture { get { return _capture.MicCapture; } }
        #endregion
    }
}
