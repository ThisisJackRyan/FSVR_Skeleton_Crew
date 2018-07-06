using System;
using System.Collections.Generic;

namespace Dissonance.Networking.Client
{
    internal class SlaveClientCollection<TPeer>
        : BaseClientCollection<TPeer?>
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(SlaveClientCollection<TPeer>).Name);

        private readonly ISendQueue<TPeer> _sender;
        private readonly ISession _session;
        private readonly EventQueue _events;
        private readonly Rooms _localRooms;
        private readonly string _playerName;

        public event Action<ClientInfo<TPeer?>> OnClientJoined;
        public event Action<ClientInfo<TPeer?>> OnClientIntroducedP2P;

        private readonly List<KeyValuePair<ushort, TPeer>> _pendingIntroductions = new List<KeyValuePair<ushort, TPeer>>();
        #endregion

        #region constructors
        public SlaveClientCollection([NotNull] ISendQueue<TPeer> sender, [NotNull] ISession session, [NotNull] EventQueue events, [NotNull] Rooms localRooms, [NotNull] string playerName)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (sender == null) throw new ArgumentNullException("sender");
            if (events == null) throw new ArgumentNullException("events");
            if (localRooms == null) throw new ArgumentNullException("localRooms");
            if (playerName == null) throw new ArgumentNullException("playerName");

            _session = session;
            _sender = sender;
            _events = events;
            _localRooms = localRooms;
            _playerName = playerName;
        }
        #endregion

        protected override void OnAddedClient(ClientInfo<TPeer?> client)
        {
            _events.EnqueuePlayerJoined(client.PlayerName);

            //Set connection property for peers who were introduced before we knew they had joined
            var done = false;
            for (var i = _pendingIntroductions.Count - 1; i >= 0; i--)
            {
                if (_pendingIntroductions[i].Key == client.PlayerId)
                {
                    if (!done)
                    {
                        var newlyMet = !client.Connection.HasValue;
                        client.Connection = _pendingIntroductions[i].Value;

                        if (newlyMet && OnClientIntroducedP2P != null)
                            OnClientIntroducedP2P(client);

                        Log.Debug("IntroduceP2P Eventual Success for '{0}' at '{1}'", client.PlayerId, client.Connection);
                        done = true;
                    }
                    _pendingIntroductions.RemoveAt(i);
                }
            }

            //Raise event
            if (OnClientJoined != null)
                OnClientJoined(client);

            base.OnAddedClient(client);
        }

        protected override void OnRemovedClient(ClientInfo<TPeer?> client)
        {
            _events.EnqueuePlayerLeft(client.PlayerName);

            base.OnRemovedClient(client);
        }

        #region packet receiving
        public void ProcessRemoveClient(ref PacketReader reader)
        {
            ushort id;
            reader.ReadRemoveClient(out id);

            ClientInfo<TPeer?> info;
            if (TryGetClientInfoById(id, out info))
                RemoveClient(info);
        }

        public void ReceiveHandshakeResponseBody(ref PacketReader reader)
        {
            // Allocation here is ok (and unavoidable). This doesn't happen very often (just once in normal circumstances) and
            // we're creating lists of peers - so at the very least we need to allocate the lists.
            var clientsByRooms = new Dictionary<ushort, List<ushort>>();
            reader.ReadHandshakeResponseBody(PlayerIds, clientsByRooms);

            //Remove all player objects who are not in the handshake response
            //Normally we would only receive one handshake response, but it's still valid to receive more
            var currentClients = new List<ClientInfo<TPeer?>>();
            GetClients(currentClients);
            for (var i = 0; i < currentClients.Count; i++)
                if (PlayerIds.GetName(currentClients[i].PlayerId) != currentClients[i].PlayerName)
                    RemoveClient(currentClients[i]);

            //Create a client object for every player which does not already exist
            foreach (var remote in PlayerIds.Items)
                GetOrCreateClientInfo(remote.Key, remote.Value, null);

            //Clear all rooms
            ClearRooms();

            //Add remote clients into rooms according to `clientsByRooms` which we deserialized from packet
            foreach (var item in clientsByRooms)
            {
                foreach (var client in item.Value)
                {
                    ClientInfo<TPeer?> info;
                    if (!TryGetClientInfoById(client, out info))
                        Log.Warn("Attempted to add an unknown client '{0}' into room '{1}'", client, item.Key);
                    else
                        JoinRoom(item.Key, info, true);
                }
            }

            //Send back a response with the complete current state of this client and follow up with deltas every time the state changes
            SendClientState();
        }
        #endregion

        #region packet sending
        private void SendClientState()
        {
            //Sanity check
            var clientId = _session.LocalId;
            if (!clientId.HasValue)
            {
                Log.Error(Log.PossibleBugMessage("Attempting to send local client state before assigned an ID by the server", "EBC361ED-780A-4DE0-944D-3D4D983B785D"));
                return;
            }

            //Send the local state
            var writer = new PacketWriter(_sender.SendBufferPool.Get());
            writer.WriteClientState(_session.SessionId, _playerName, clientId.Value, _localRooms);
            _sender.EnqueueReliable(writer.Written);
            Log.Debug("Sent local client state");

            //begin watching for changes in rooms
            _localRooms.JoinedRoom -= SendJoinRoom;
            _localRooms.JoinedRoom += SendJoinRoom;
            _localRooms.LeftRoom -= SendLeaveRoom;
            _localRooms.LeftRoom += SendLeaveRoom;
        }

        private void SendLeaveRoom(string room)
        {
            //Sanity check
            var id = _session.LocalId;
            if (id == null)
            {
                Log.Error(Log.PossibleBugMessage("Attempted to send channel state delta, but local client ID is null", "7F29AD74-7F03-46BA-A776-F63F25A39FC5"));
                return;
            }

            var writer = new PacketWriter(_sender.SendBufferPool.Get());
            writer.WriteDeltaChannelState(_session.SessionId, false, id.Value, room.ToRoomId());

            _sender.EnqueueReliable(writer.Written);
        }

        private void SendJoinRoom(string room)
        {
            //Sanity check
            var id = _session.LocalId;
            if (id == null)
            {
                Log.Error(Log.PossibleBugMessage("Attempted to send channel state delta, but local client ID is null", "73A33580-B876-4D16-9578-5FB417BA98F5"));
                return;
            }

            var writer = new PacketWriter(_sender.SendBufferPool.Get());
            writer.WriteDeltaChannelState(_session.SessionId, true, id.Value, room.ToRoomId());

            _sender.EnqueueReliable(writer.Written);
        }
        #endregion

        public void Stop()
        {
            _localRooms.JoinedRoom -= SendJoinRoom;
            _localRooms.LeftRoom -= SendLeaveRoom;
        }

        public void IntroduceP2P(ushort id, TPeer connection)
        {
            if (!TryIntroduceP2P(id, connection))
            {
                Log.Debug("IntroduceP2P not yet complete for '{0}' at '{1}'", id, connection);
                _pendingIntroductions.Add(new KeyValuePair<ushort, TPeer>(id, connection));
            }
        }

        private bool TryIntroduceP2P(ushort id, TPeer connection)
        {
            ClientInfo<TPeer?> info;
            if (TryGetClientInfoById(id, out info))
            {
                Log.Debug("IntroduceP2P Success for '{0}' at '{1}'", id, connection);

                var newlyMet = !info.Connection.HasValue;
                info.Connection = connection;

                //raise event
                if (newlyMet && OnClientIntroducedP2P != null)
                    OnClientIntroducedP2P(info);

                return true;
            }

            return false;
        }
    }
}
