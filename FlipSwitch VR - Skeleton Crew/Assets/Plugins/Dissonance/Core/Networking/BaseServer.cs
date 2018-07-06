using System;
using System.Collections.Generic;
using Dissonance.Networking.Server;

namespace Dissonance.Networking
{
    public abstract class BaseServer<TServer, TClient, TPeer>
        : IServer<TPeer>
        where TPeer : struct
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
    {
        #region fields and properties
        protected readonly Log Log;

        private bool _disconnected;

        internal TrafficCounter RecvHandshakeRequest { get; private set; }
        internal TrafficCounter RecvClientState { get; private set; }
        internal TrafficCounter RecvPacketRelay { get; private set; }
        internal TrafficCounter RecvDeltaChannelState { get; private set; }
        internal TrafficCounter SentTraffic { get; private set; }

        private readonly ServerRelay<TPeer> _relay;
        private readonly BroadcastingClientCollection<TPeer> _clients;

        private readonly uint _sessionId;
        public uint SessionId { get { return _sessionId; } }
        #endregion

        #region constructors
        protected BaseServer()
        {
            Log = Logs.Create(LogCategory.Network, GetType().Name);

            RecvClientState = new TrafficCounter();
            RecvHandshakeRequest = new TrafficCounter();
            RecvPacketRelay = new TrafficCounter();
            SentTraffic = new TrafficCounter();
            RecvDeltaChannelState = new TrafficCounter();

            _sessionId = unchecked((uint)new Random().Next());

            _clients = new BroadcastingClientCollection<TPeer>(this);
            _relay = new ServerRelay<TPeer>(this, _clients);

            Log.Info("Constructing host. SessionId:{0}", _sessionId);
        }
        #endregion

        /// <summary>
        /// Perform any initial work required to connect
        /// </summary>
        public virtual void Connect()
        {
            Log.Info("Connected");
        }

        /// <summary>
        /// Perform any teardown work required to disconnect
        /// </summary>
        public virtual void Disconnect()
        {
            if (_disconnected)
                return;
            _disconnected = true;

            _clients.Clear();

            Log.Info("Disconnected");
        }

        /// <summary>
        /// This must be called by the extending network integration implementation when a client disconnects from the session
        /// </summary>
        /// <param name="connection"></param>
        protected void ClientDisconnected(TPeer connection)
        {
            Log.Debug("Received disconnection event for peer '{0}'", connection);

            _clients.RemoveClient(connection);
        }

        public virtual ServerState Update()
        {
            if (_disconnected)
                return ServerState.Error;

            ReadMessages();
            return ServerState.Ok;
        }

        #region sending
        /// <summary>
        /// Send a control packet (reliable, in-order) to the given destination
        /// </summary>
        /// <param name="connection">Destination</param>
        /// <param name="packet">Packet to send</param>
        protected abstract void SendReliable(TPeer connection, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable packet (unreliable, unordered) to the given destination
        /// </summary>
        /// <param name="connection">Destination</param>
        /// <param name="packet">Packet to send</param>
        protected abstract void SendUnreliable(TPeer connection, ArraySegment<byte> packet);

        void IServer<TPeer>.SendUnreliable(TPeer connection, ArraySegment<byte> packet)
        {
            SentTraffic.Update(packet.Count);
            SendUnreliable(connection, packet);
        }

        public virtual void SendUnreliable(List<TPeer> connections, ArraySegment<byte> packet)
        {
            SentTraffic.Update(packet.Count * connections.Count);

            for (var i = 0; i < connections.Count; i++)
                SendUnreliable(connections[i], packet);
        }

        void IServer<TPeer>.SendReliable(TPeer connection, ArraySegment<byte> packet)
        {
            SentTraffic.Update(packet.Count);
            SendReliable(connection, packet);
        }

        public virtual void SendReliable(List<TPeer> connections, ArraySegment<byte> packet)
        {
            SentTraffic.Update(packet.Count * connections.Count);

            for (var i = 0; i < connections.Count; i++)
                SendReliable(connections[i], packet);
        }
        #endregion

        #region packet processing
        /// <summary>
        /// Read messages (call NetworkReceivedPacket with all messages)
        /// </summary>
        protected abstract void ReadMessages();

        /// <summary>
        /// Receive a packet from the network for dissonance
        /// </summary>
        /// <param name="source">An integer identifying where this packet came from (same ID will be used for sending)</param>
        /// <param name="data">Packet received</param>
        public void NetworkReceivedPacket(TPeer source, ArraySegment<byte> data)
        {
            var reader = new PacketReader(data);

            var magic = reader.ReadUInt16();
            if (magic != PacketWriter.Magic)
            {
                Log.Warn("Received packet with incorrect magic number. Expected {0}, got {1}. Ignoring packet.", PacketWriter.Magic, magic);
                return;
            }

            var header = (MessageTypes)reader.ReadByte();
            if (header != MessageTypes.HandshakeRequest)
            {
                var session = reader.ReadUInt32();
                if (session != _sessionId)
                {
                    Log.Warn("Received a packet with incorrect session ID. Expected {0}, got {1}. Resetting client.", _sessionId, session);

                    //Send back a packet forcing this client to disconnect and reconnect, rerunning the handshake stage to acquire the correct session ID
                    var writer = new PacketWriter(new byte[7]);
                    writer.WriteErrorWrongSession(session);
                    SendReliable(source, writer.Written);

                    return;
                }
            }

            switch (header)
            {
                case MessageTypes.HandshakeRequest:
                    RecvHandshakeRequest.Update(data.Count);
                    ClientDisconnected(source);                     // Disconnect existing peers on this connection
                    _clients.ProcessHandshakeRequest(source, ref reader);
                    break;

                case MessageTypes.ClientState:
                    RecvClientState.Update(data.Count);
                    _clients.ProcessClientState(source, ref reader);
                    break;

                case MessageTypes.ServerRelayReliable:
                case MessageTypes.ServerRelayUnreliable:
                    RecvPacketRelay.Update(data.Count);
                    _relay.ProcessPacketRelay(ref reader, header == MessageTypes.ServerRelayReliable);
                    break;

                case MessageTypes.DeltaChannelState:
                    RecvDeltaChannelState.Update(data.Count);
                    _clients.ProcessDeltaChannelState(ref reader);
                    break;

                case MessageTypes.RemoveClient:
                case MessageTypes.VoiceData:
                case MessageTypes.TextData:
                case MessageTypes.HandshakeResponse:
                case MessageTypes.ErrorWrongSession:
                    Log.Error("Server received packet '{0}'. This should only ever be received by the client", header);
                    break;

                default:
                    Log.Error("Ignoring a packet with an unknown header: '{0}'", header);
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Called whenever a new client joins the session. Override to perform some work on this event
        /// </summary>
        /// <param name="client"></param>
        protected virtual void AddClient(ClientInfo<TPeer> client)
        {
        }

        void IServer<TPeer>.AddClient(ClientInfo<TPeer> client)
        {
            AddClient(client);
        }
    }
}
