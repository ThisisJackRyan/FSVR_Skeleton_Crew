using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking.Client;

namespace Dissonance.Networking
{
    public abstract class BaseClient<TServer, TClient, TPeer>
        : IClient<TPeer>
        where TPeer : struct
        where TServer : BaseServer<TServer, TClient, TPeer>
        where TClient : BaseClient<TServer, TClient, TPeer>
    {
        #region fields and properties
        protected readonly Log Log;

        private bool _disconnected;
        private bool _error;

        public bool IsConnected { get { return _serverNegotiator.State == ConnectionState.Connected; } }

        private readonly EventQueue _events;
        public event Action<string> PlayerJoined
        {
            add { _events.PlayerJoined += value; }
            remove { _events.PlayerJoined -= value; }
        }
        public event Action<string> PlayerLeft
        {
            add { _events.PlayerLeft += value; }
            remove { _events.PlayerLeft -= value; }
        }
        public event Action<VoicePacket> VoicePacketReceived
        {
            add { _events.VoicePacketReceived += value; }
            remove { _events.VoicePacketReceived -= value; }
        }
        public event Action<TextMessage> TextMessageReceived
        {
            add { _events.TextMessageReceived += value; }
            remove { _events.TextMessageReceived -= value; }
        }
        public event Action<string> PlayerStartedSpeaking
        {
            add { _events.PlayerStartedSpeaking += value; }
            remove { _events.PlayerStartedSpeaking -= value; }
        }
        public event Action<string> PlayerStoppedSpeaking
        {
            add { _events.PlayerStoppedSpeaking += value; }
            remove { _events.PlayerStoppedSpeaking -= value; }
        }

        private readonly SlaveClientCollection<TPeer> _peers;
        private readonly ConnectionNegotiator<TPeer> _serverNegotiator;
        private readonly SendQueue<TPeer> _sendQueue;
        private readonly PacketDelaySimulator _lossSimulator;

        private readonly VoiceReceiver<TPeer> _voiceReceiver;
        private readonly VoiceSender<TPeer> _voiceSender;
        private readonly TextReceiver<TPeer> _textReceiver;
        private readonly TextSender<TPeer> _textSender;

        private readonly TrafficCounter _recvRemoveClient = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvRemoveClient { get { return _recvRemoveClient; } }
        private readonly TrafficCounter _recvVoiceData = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvVoiceData { get { return _recvVoiceData; } }
        private readonly TrafficCounter _recvTextData = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvTextData { get { return _recvTextData; } }
        private readonly TrafficCounter _recvHandshakeResponse = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvHandshakeResponse { get { return _recvHandshakeResponse; } }
        private readonly TrafficCounter _recvHandshakeP2P = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvHandshakeP2P { get { return _recvHandshakeP2P; } }
        private readonly TrafficCounter _recvClientState = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvClientState { get { return _recvClientState; } }
        private readonly TrafficCounter _recvDeltaState = new TrafficCounter();
        [NotNull] internal TrafficCounter RecvDeltaState { get { return _recvDeltaState; } }
        private readonly TrafficCounter _sentServer = new TrafficCounter();
        [NotNull] internal TrafficCounter SentServerTraffic { get { return _sentServer; } }
        private readonly TrafficCounter _sentP2P = new TrafficCounter();
        [NotNull] internal TrafficCounter SentPeerTraffic { get { return _sentP2P; } }
        #endregion

        #region constructors
        protected BaseClient([NotNull] ICommsNetworkState network)
        {
            Log = Logs.Create(LogCategory.Network, GetType().Name);

            const int poolSize = 32;
            const int bufferSize = 1024;
            var pool = new ConcurrentPool<byte[]>(poolSize, () => new byte[bufferSize]);
                
            _sendQueue = new SendQueue<TPeer>(this, pool);
            _serverNegotiator = new ConnectionNegotiator<TPeer>(_sendQueue, network.PlayerName);
            _lossSimulator = new PacketDelaySimulator();

            _events = new EventQueue(pool);
            _peers = new SlaveClientCollection<TPeer>(_sendQueue, _serverNegotiator, _events, network.Rooms, network.PlayerName);
            _peers.OnClientJoined += OnAddedClient;
            _peers.OnClientIntroducedP2P += OnMetClient;

            _voiceReceiver = new VoiceReceiver<TPeer>(_serverNegotiator, _peers, _events, network.Rooms, pool);
            _voiceSender = new VoiceSender<TPeer>(_sendQueue, _serverNegotiator, _peers, _events, network.PlayerChannels, network.RoomChannels);

            _textReceiver = new TextReceiver<TPeer>(_events, network.Rooms, _peers);
            _textSender = new TextSender<TPeer>(_sendQueue, _serverNegotiator, _peers);
        }
        #endregion

        #region connect/disconnect
        /// <summary>
        /// Override this to perform any work necessary to join a voice session
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Call this once work has been done and we are now in a voice session
        /// </summary>
        protected void Connected()
        {
            _serverNegotiator.Start();
        }

        /// <summary>
        /// Override this to perform any work necessary to leave a voice session
        /// </summary>
        public virtual void Disconnect()
        {
            if (_disconnected)
                return;
            _disconnected = true;

            _sendQueue.Stop();
            _serverNegotiator.Stop();
            _peers.Stop();
            _voiceReceiver.Stop();
            _voiceSender.Stop();

            _events.DispatchEvents();

            Log.Info("Disconnected");
        }
        #endregion

        public virtual ClientStatus Update()
        {
            if (_disconnected)
                return ClientStatus.Error;

            if (_error)
                return ClientStatus.Error;

            try
            {
                //Update components
                _serverNegotiator.Update();
                _sendQueue.Update();
                _voiceReceiver.Update();

                //Poll network layer for more packets
                ReadMessages();

                return ClientStatus.Ok;
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                return ClientStatus.Error;
            }
            finally
            {
                //Send events to event handlers
                if (_events.DispatchEvents())
                    _error = true;
            }
        }

        #region send
        /// <summary>
        /// Send a packet of voice data from this client
        /// </summary>
        /// <param name="encodedAudio"></param>
        public void SendVoiceData(ArraySegment<byte> encodedAudio)
        {
            _voiceSender.Send(encodedAudio);
        }

        public void SendTextData(string data, ChannelType type, string recipient)
        {
            _textSender.Send(data, type, recipient);
        }
        #endregion

        #region receive
        public ushort? NetworkReceivedPacket(ArraySegment<byte> data)
        {
            if (_lossSimulator.ShouldLose(data))
                return null;

            return ProcessReceivedPacket(data);
        }

        private ushort? ProcessReceivedPacket(ArraySegment<byte> data)
        {
            var reader = new PacketReader(data);

            var magic = reader.ReadUInt16();
            if (magic != PacketWriter.Magic)
            {
                Log.Warn("Received packet with incorrect magic number. Expected {0}, got {1}", PacketWriter.Magic, magic);
                return null;
            }

            var header = (MessageTypes)reader.ReadByte();
            switch (header)
            {
                case MessageTypes.VoiceData:
                    if (CheckSessionId(ref reader))
                    {
                        _voiceReceiver.ReceiveVoiceData(ref reader);
                        _recvVoiceData.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.TextData:
                    if (CheckSessionId(ref reader))
                    {
                        _textReceiver.ProcessTextMessage(ref reader);
                        _recvTextData.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.HandshakeResponse:
                    _serverNegotiator.ReceiveHandshakeResponseHeader(ref reader);
                    _peers.ReceiveHandshakeResponseBody(ref reader);
                    _recvHandshakeResponse.Update(reader.Read.Count);

                    if (_serverNegotiator.LocalId.HasValue)
                        OnServerAssignedSessionId(_serverNegotiator.SessionId, _serverNegotiator.LocalId.Value);

                    break;

                case MessageTypes.RemoveClient:
                    if (CheckSessionId(ref reader))
                    {
                        _peers.ProcessRemoveClient(ref reader);
                        _recvRemoveClient.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.ClientState:
                    if (CheckSessionId(ref reader))
                    {
                        _peers.ProcessClientState(null, ref reader);
                        _recvClientState.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.DeltaChannelState:
                    if (CheckSessionId(ref reader))
                    {
                        _peers.ProcessDeltaChannelState(ref reader);
                        _recvDeltaState.Update(reader.Read.Count);
                    }
                    break;

                case MessageTypes.ErrorWrongSession:
                    if (!CheckSessionId(ref reader))
                    {
                        Log.Error("Kicked from session - wrong session ID");
                        _error = true;
                    }
                    break;

                case MessageTypes.HandshakeP2P:
                    if (CheckSessionId(ref reader))
                    {
                        ushort id;
                        reader.ReadhandshakeP2P(out id);
                        return id;
                    }
                    break;

                case MessageTypes.ServerRelayReliable:
                case MessageTypes.ServerRelayUnreliable:
                case MessageTypes.HandshakeRequest:
                    Log.Error("Client received packet '{0}'. This should only ever be received by the server", header);
                    break;

                default:
                    Log.Error("Ignoring a packet with an unknown header: '{0}'", header);
                    break;
            }

            return null;
        }

        private bool CheckSessionId(ref PacketReader reader)
        {
            var session = reader.ReadUInt32();

            if (_serverNegotiator.SessionId != session)
            {
                Log.Warn("Received a packet with incorrect session ID. Expected {0}, got {1}", _serverNegotiator.SessionId, session);
                return false;
            }

            return true;
        }
        #endregion

        #region abstract
        /// <summary>
        /// Read messages from the network layer and call `NetworkReceivedPacket` with each packet
        /// </summary>
        protected abstract void ReadMessages();

        /// <summary>
        /// Send a reliable message to the server
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void SendReliable(ArraySegment<byte> packet);

        /// <summary>
        /// send an unreliable message to the server
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void SendUnreliable(ArraySegment<byte> packet);
        #endregion

        #region p2p
        protected virtual void SendReliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            //Since we're calling the base implementation of send P2P we'll just relay this packet by the server

            if (destinations.Count > 0)
            {
                //We're going to server relay, so update server counter
                SentServerTraffic.Update(packet.Count);

                //Get a buffer to write the relay packet into
                var buffer = _sendQueue.SendBufferPool.Get();
                {
                    //Write relay packet
                    var writer = new PacketWriter(buffer);
                    writer.WriteRelay(_serverNegotiator.SessionId, destinations, packet, true);

                    //Send relay packet
                    SendReliable(writer.Written);
                }
                //Recycle relay buffer
                _sendQueue.SendBufferPool.Put(buffer);
            }
        }

        protected virtual void SendUnreliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            //Since we're calling the base implementation of send P2P we'll just relay this packet by the server

            if (destinations.Count > 0)
            {
                //We're going to server relay, so update server counter
                SentServerTraffic.Update(packet.Count);

                //Get a buffer to write the relay packet into
                var buffer = _sendQueue.SendBufferPool.Get();
                {
                    //Write relay packet
                    var writer = new PacketWriter(buffer);
                    writer.WriteRelay(_serverNegotiator.SessionId, destinations, packet, true);

                    //Send relay packet
                    SendUnreliable(writer.Written);
                }
                //Recycle relay buffer
                _sendQueue.SendBufferPool.Put(buffer);
            }
        }

        protected virtual void OnServerAssignedSessionId(uint session, ushort id)
        {

        }

        /// <summary>
        /// Called when a new client is added into the session (we may not know how to directly to talk to them yet, but server relay is available)
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnAddedClient([NotNull] ClientInfo<TPeer?> client)
        {
        }

        /// <summary>
        /// Called when a client has had a connection assigned, meaning we can now communicate with them directly
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnMetClient([NotNull] ClientInfo<TPeer?> client)
        {
            if (client.Connection.HasValue)
            {
                SendHandshakeP2P(client);
            }
        }

        /// <summary>
        /// Call this to inform Dissonance how to directly contact a peer
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connection"></param>
        protected void ReceiveHandshakeP2P(ushort id, TPeer connection)
        {
            if (!IsConnected)
            {
                Log.Error("Attempted to call IntroduceP2P before connected to Dissonance session");
                return;
            }

            _peers.IntroduceP2P(id, connection);
        }

        private void SendHandshakeP2P(ClientInfo<TPeer?> connection)
        {
            if (!IsConnected)
            {
                Log.Error("Attempted to call IntroduceP2P before connected to Dissonance session");
                return;
            }

            if (!_serverNegotiator.LocalId.HasValue)
            {
                Log.Error(Log.PossibleBugMessage("No LocalId assigned even though server negotiator is connected", "C89CC5D4-3346-442B-9A44-E7BEDA822610"));
                return;
            }

            //Can't send a handshake if we can't communicate directly with them
            if (!connection.Connection.HasValue)
                return;

            var writer = new PacketWriter(_sendQueue.SendBufferPool.Get()).WriteHandshakeP2P(_serverNegotiator.SessionId, _serverNegotiator.LocalId.Value);

            SendReliableP2P(new List<ClientInfo<TPeer?>> { connection }, writer.Written);
        }

        protected static byte[] WriteHandshakeP2P(uint sessionId, ushort clientId)
        {
            var segment = new PacketWriter(new byte[9])
                .WriteHandshakeP2P(sessionId, clientId)
                .Written;

            // ReSharper disable once PossibleNullReferenceException (Justification: `Array` cannot be null)
            if (segment.Count != segment.Array.Length)
                return segment.ToArray();
            else
                return segment.Array;

        }
        #endregion

        #region IClient explicit impl
        void IClient<TPeer>.SendReliable(ArraySegment<byte> packet)
        {
            SendReliable(packet);
        }

        void IClient<TPeer>.SendUnreliable(ArraySegment<byte> packet)
        {
            SendUnreliable(packet);
        }

        void IClient<TPeer>.SendReliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendReliableP2P(destinations, packet);
        }

        void IClient<TPeer>.SendUnreliableP2P([NotNull] List<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            SendUnreliableP2P(destinations, packet);
        }
        #endregion
    }
}
