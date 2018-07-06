using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Audio.Capture;

namespace Dissonance
{
    internal class PlayerCollection
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Core, typeof(PlayerCollection).Name);

        private readonly Dictionary<string, VoicePlayerState> _playersLookup = new Dictionary<string, VoicePlayerState>();
        private readonly List<VoicePlayerState> _players = new List<VoicePlayerState>();

        private readonly ReadOnlyCollection<VoicePlayerState> _playersReadOnly;
        [NotNull] public ReadOnlyCollection<VoicePlayerState> Readonly { get { return _playersReadOnly; } }

        public LocalVoicePlayerState Local { get; private set; }
        #endregion

        public PlayerCollection()
        {
            _playersReadOnly = new ReadOnlyCollection<VoicePlayerState>(_players);
        }

        public void Start(string name, IMicrophoneProvider micProvider, RoomChannels roomChannels, PlayerChannels playerChannels)
        {
            if (name == null)
                throw new ArgumentException("name");
            if (micProvider == null)
                throw new ArgumentException("micProvider");
            if (roomChannels == null)
                throw new ArgumentException("roomChannels");
            if (playerChannels == null)
                throw new ArgumentException("playerChannels");

            Local = new LocalVoicePlayerState(name, micProvider, roomChannels, playerChannels);

            Add(Local);
        }

        public void Add(VoicePlayerState state)
        {
            if (state == null)
                throw new ArgumentNullException("state");

            if (_playersLookup.ContainsKey(state.Name))
                throw Log.CreatePossibleBugException("Attempted to add a duplicate player to the player collection", "1AA3B631-9813-4FDA-878B-06CD2226C179");

            _players.Add(state);
            _playersLookup.Add(state.Name, state);
        }

        [CanBeNull] public VoicePlayerState Remove(string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");
            if (Local != null && playerId == Local.Name)
                throw new InvalidOperationException("Cannot remove local player from player collection");

            VoicePlayerState state;
            if (!_playersLookup.TryGetValue(playerId, out state))
                return null;

            //Remove from dictionary
            _playersLookup.Remove(playerId);

            //Remove from list
            for (var i = _players.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_players[i].Name, playerId))
                {
                    _players.RemoveAt(i);
                    break;
                }
            }

            return state;
        }

        public bool TryGet(string playerId, out VoicePlayerState state)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");

            return _playersLookup.TryGetValue(playerId, out state);
        }

        public void Update()
        {
            for (var i = 0; i < _players.Count; i++)
                _players[i].Update();
        }
    }
}
