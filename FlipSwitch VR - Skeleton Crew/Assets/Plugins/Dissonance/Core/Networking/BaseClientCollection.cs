using System;
using System.Collections.Generic;

namespace Dissonance.Networking
{
    internal class BaseClientCollection<TPeer>
        : IClientCollection<TPeer>
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(BaseClientCollection<TPeer>).Name);

        protected readonly ClientIdCollection PlayerIds = new ClientIdCollection();

        protected readonly Dictionary<ushort, List<ClientInfo<TPeer>>> ClientsByRoom = new Dictionary<ushort, List<ClientInfo<TPeer>>>();

        private readonly Dictionary<ushort, ClientInfo<TPeer>> _clientsByPlayerId = new Dictionary<ushort, ClientInfo<TPeer>>();
        private readonly Dictionary<string, ClientInfo<TPeer>> _clientsByName = new Dictionary<string, ClientInfo<TPeer>>();
        #endregion

        public void Clear()
        {
            PlayerIds.Clear();
            ClientsByRoom.Clear();
            _clientsByPlayerId.Clear();
        }

        #region add/remove clients
        protected virtual void OnAddedClient([NotNull] ClientInfo<TPeer> client)
        {
        }

        protected virtual void OnRemovedClient([NotNull] ClientInfo<TPeer> client)
        {
        }

        [NotNull] protected ClientInfo<TPeer> GetOrCreateClientInfo(ushort id, [NotNull] string name, [CanBeNull] TPeer connection)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            ClientInfo<TPeer> info;
            if (TryGetClientInfoById(id, out info))
                return info;

            info = new ClientInfo<TPeer>(name, id, connection);
            _clientsByPlayerId[id] = info;
            _clientsByName[name] = info;

            OnAddedClient(info);

            return info;
        }

        protected void RemoveClient([NotNull] ClientInfo<TPeer> client)
        {
            Log.Debug("Removing client '{0}'", client.PlayerName);

            //Set the flag to indicate that this client is gone
            client.IsConnected = false;

            //Remove from player ID collection
            PlayerIds.Unregister(client.PlayerName);

            //Remove from client sets
            _clientsByPlayerId.Remove(client.PlayerId);
            _clientsByName.Remove(client.PlayerName);

            //Remove from room membership lists
            for (var i = client.Rooms.Count - 1; i >= 0; i--)
                LeaveRoom(client.Rooms[i], client);

            //Raise the removal event
            OnRemovedClient(client);
        }
        #endregion

        #region query
        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        public bool TryGetClientInfoById(ushort player, out ClientInfo<TPeer> info)
        {
            return _clientsByPlayerId.TryGetValue(player, out info);
        }

        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        public bool TryGetClientInfoByName([CanBeNull] string name, out ClientInfo<TPeer> info)
        {
            if (name == null)
            {
                // ReSharper disable once AssignNullToNotNullAttribute (Justification: this is within the method contract)
                info = null;
                return false;
            }

            return _clientsByName.TryGetValue(name, out info);
        }

        [ContractAnnotation("=> true, clients:notnull; => false, clients:null")]
        public bool TryGetClientsInRoom(ushort room, out List<ClientInfo<TPeer>> clients)
        {
            return ClientsByRoom.TryGetValue(room, out clients);
        }

        protected void GetClients(List<ClientInfo<TPeer>> output)
        {
            using (var enumerator = _clientsByPlayerId.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    output.Add(enumerator.Current);
            }
        }

        [ContractAnnotation("=> true, info:notnull; => false, info:null")]
        protected bool TryFindClientByConnection(TPeer connection, [CanBeNull] out ClientInfo<TPeer> info)
        {
            using (var enumerator = _clientsByPlayerId.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    if (item != null && connection.Equals(item.Connection))
                    {
                        info = item;
                        return true;
                    }
                }
            }

            info = null;
            return false;
        }
        #endregion

        #region room management
        protected void ClearRooms()
        {
            foreach (var kvp in ClientsByRoom)
                kvp.Value.Clear();
        }

        protected void JoinRoom(ushort room, ClientInfo<TPeer> client, bool addToClientList = true)
        {
            List<ClientInfo<TPeer>> clientListForRoom;
            if (!ClientsByRoom.TryGetValue(room, out clientListForRoom))
            {
                clientListForRoom = new List<ClientInfo<TPeer>>();
                ClientsByRoom[room] = clientListForRoom;
            }

            clientListForRoom.Add(client);

            if (addToClientList)
                client.Rooms.Add(room);
        }

        private void LeaveRoom(ushort room, ClientInfo<TPeer> client, bool removeFromClientList = true)
        {
            List<ClientInfo<TPeer>> clientListForRoom;
            if (ClientsByRoom.TryGetValue(room, out clientListForRoom))
                clientListForRoom.Remove(client);

            if (removeFromClientList)
                client.Rooms.Remove(room);
        }
        #endregion

        #region packet processing
        public virtual void ProcessClientState([CanBeNull] TPeer source, ref PacketReader reader)
        {
            //Read header to identity which client this is
            string name;
            ushort id;
            reader.ReadClientStateHeader(out name, out id);

            //Get or create the info object for this client
            var info = GetOrCreateClientInfo(id, name, source);

            //Remove this client from all rooms
            for (var i = 0; i < info.Rooms.Count; i++)
                LeaveRoom(info.Rooms[i], info, removeFromClientList: false);
            info.Rooms.Clear();

            //Read the rooms this client is in
            reader.ReadClientStateRooms(info);

            //Add client to rooms as necessary
            for (var i = 0; i < info.Rooms.Count; i++)
                JoinRoom(info.Rooms[i], info, addToClientList: false);
        }

        public virtual void ProcessDeltaChannelState(ref PacketReader reader)
        {
            bool joined;
            ushort peer;
            ushort room;
            reader.ReadDeltaChannelState(out joined, out peer, out room);

            ClientInfo<TPeer> info;
            if (!TryGetClientInfoById(peer, out info))
            {
                Log.Warn("Received a DeltaChannelState for an unknown peer");
                return;
            }

            if (joined)
                JoinRoom(room, info);
            else
                LeaveRoom(room, info);
        }
        #endregion
    }

    internal interface IClientCollection<TPeer>
    {
        bool TryGetClientInfoById(ushort clientId, out ClientInfo<TPeer> info);

        bool TryGetClientInfoByName(string clientName, out ClientInfo<TPeer> info);

        bool TryGetClientsInRoom(ushort roomId, out List<ClientInfo<TPeer>> clients);
    }
}
