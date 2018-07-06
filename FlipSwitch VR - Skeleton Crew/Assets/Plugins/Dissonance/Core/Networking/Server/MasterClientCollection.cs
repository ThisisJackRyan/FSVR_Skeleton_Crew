using System;
using System.Collections.Generic;

namespace Dissonance.Networking.Server
{
    /// <summary>
    /// A client collection which assigns peer IDs and broadcasts all state changes
    /// </summary>
    /// <typeparam name="TPeer"></typeparam>
    internal class BroadcastingClientCollection<TPeer>
        : BaseClientCollection<TPeer>
    {
        #region fields and properties
        private readonly Log Log = Logs.Create(LogCategory.Network, typeof(BroadcastingClientCollection<TPeer>).Name);

        private readonly IServer<TPeer> _server;

        private readonly byte[] _tmpSendBuffer = new byte[1024];
        private readonly List<TPeer> _tmpConnectionBuffer = new List<TPeer>();
        private readonly List<ClientInfo<TPeer>> _tmpClientBuffer = new List<ClientInfo<TPeer>>();
        #endregion

        #region constructor
        public BroadcastingClientCollection(IServer<TPeer> server)
        {
            _server = server;
        }
        #endregion

        protected override void OnRemovedClient(ClientInfo<TPeer> client)
        {
            base.OnRemovedClient(client);

            //Write the removal message
            var writer = new PacketWriter(_tmpSendBuffer);
            writer.WriteRemoveClient(_server.SessionId, client.PlayerId);

            //Broadcast to all peers
            Broadcast(writer.Written);
        }

        protected override void OnAddedClient(ClientInfo<TPeer> client)
        {
            base.OnAddedClient(client);

            _server.AddClient(client);
        }

        #region packet processing
        public void ProcessHandshakeRequest(TPeer source, ref PacketReader reader)
        {
            //Parse packet
            string name;
            reader.ReadHandshakeRequest(out name);
            if (name == null)
            {
                Log.Warn("Ignoring a handshake with a null player name");
                return;
            }

            //Get or register the ID for this client
            var id = PlayerIds.GetId(name) ?? PlayerIds.Register(name);
            var info = GetOrCreateClientInfo(id, name, source);

            //Send back response
            var writer = new PacketWriter(_tmpSendBuffer);
            writer.WriteHandshakeResponse(_server.SessionId, info.PlayerId, PlayerIds, ClientsByRoom);
            _server.SendReliable(source, writer.Written);
        }

        public override void ProcessClientState(TPeer source, ref PacketReader reader)
        {
            //Rebroadcast packet to all peers so they can update their state
            Broadcast(reader.All);

            base.ProcessClientState(source, ref reader);
        }

        public override void ProcessDeltaChannelState(ref PacketReader reader)
        {
            //Rebroadcast packet to all peers so they can update their state
            Broadcast(reader.All);

            base.ProcessDeltaChannelState(ref reader);
        }
        #endregion

        private void Broadcast(ArraySegment<byte> packet)
        {
            _tmpConnectionBuffer.Clear();
            _tmpClientBuffer.Clear();

            //Get all client infos
            GetClients(_tmpClientBuffer);

            //Now get all connections (except the one excluded one, if applicable)
            for (var i = 0; i < _tmpClientBuffer.Count; i++)
            {
                var c = _tmpClientBuffer[i];
                _tmpConnectionBuffer.Add(c.Connection);
            }

            //Broadcast to all those connections
            _server.SendReliable(_tmpConnectionBuffer, packet);

            _tmpConnectionBuffer.Clear();
            _tmpClientBuffer.Clear();
        }

        public void RemoveClient(TPeer connection)
        {
            ClientInfo<TPeer> info;
            if (TryFindClientByConnection(connection, out info))
                RemoveClient(info);
        }

        public ClientInfo<TPeer> Create(string name, TPeer connection)
        {
            return GetOrCreateClientInfo(PlayerIds.Register(name), name, connection);
        }
    }
}
