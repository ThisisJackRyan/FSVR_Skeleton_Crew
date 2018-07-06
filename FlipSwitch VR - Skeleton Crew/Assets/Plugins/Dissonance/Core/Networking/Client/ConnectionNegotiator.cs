using System;
using System.Threading;

namespace Dissonance.Networking.Client
{
    internal enum ConnectionState
    {
        None,
        Negotiating,
        Connected,
        Disconnected
    }

    internal class ConnectionNegotiator<TPeer>
        : ISession
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ConnectionNegotiator<TPeer>).Name);
        private static readonly TimeSpan HandshakeRequestInterval = TimeSpan.FromSeconds(2);

        private readonly ISendQueue<TPeer> _sender;
        private readonly string _playerName;

        private DateTime _lastHandshakeRequest = DateTime.MinValue;
        private bool _running;

        private int _connectionStateValue = (int)ConnectionState.None;
        public ConnectionState State { get { return (ConnectionState)_connectionStateValue; } }

        public uint SessionId { get; private set; }
        public ushort? LocalId { get; private set; }
        #endregion

        public ConnectionNegotiator([NotNull] ISendQueue<TPeer> sender, string playerName)
        {
            _sender = sender;
            _playerName = playerName;
        }

        public void ReceiveHandshakeResponseHeader(ref PacketReader reader)
        {
            uint session;
            ushort myId;
            reader.ReadHandshakeResponseHeader(out session, out myId);

            //Save local client info as assigned by the server
            SessionId = session;
            LocalId = myId;

            //We could receive an unbounded number of handshake responses. We only want to run this event on the *first* one (when we transition from Negotiating to Connected
            //Additionally it's possible the connection is not in the negotiating state (could already be disconnected). So check that it's the right value before exchanging.
            if (Interlocked.CompareExchange(ref _connectionStateValue, (int)ConnectionState.Connected, (int)ConnectionState.Negotiating) == (int)ConnectionState.Negotiating)
                Log.Info("Received handshake response from server, joined session '{0}'", SessionId);
        }

        public void Start()
        {
            if (State == ConnectionState.Disconnected)
                throw Log.CreatePossibleBugException("Attempted to restart a ConnectionNegotiator after it has been disconnected", "92F0B2EB-282A-4558-B3BD-6656F83A06E3");

            _running = true;
        }

        public void Stop()
        {
            _running = false;
            _connectionStateValue = (int)ConnectionState.Disconnected;
        }

        public void Update()
        {
            if (!_running)
                return;

            var shouldResendHandshake = State == ConnectionState.Negotiating && DateTime.Now - _lastHandshakeRequest > HandshakeRequestInterval;
            if (State == ConnectionState.None || shouldResendHandshake)
                SendHandshake();
        }

        /// <summary>
        /// Begin negotiating a connection with the server by sending a handshake.
        /// </summary>
        /// <remarks>It is safe to call this several times, even once negotiation has finished</remarks>
        private void SendHandshake()
        {
            //Sanity check. We can't do *anything* with a disconnected client, definitely not restart negotiation!
            if (State == ConnectionState.Disconnected)
                throw Log.CreatePossibleBugException("Attempted to begin connection negotiation with a client which is disconnected", "39533F23-2DAC-4340-9A7D-960904464E23");

            _lastHandshakeRequest = DateTime.Now;

            //Send the handshake request to the server (when the server replies with a response, we know we're connected)
            _sender.EnqueueReliable(
                new PacketWriter(new ArraySegment<byte>(_sender.SendBufferPool.Get()))
                    .WriteHandshakeRequest(_playerName)
                    .Written
            );

            //Set the state to negotiating only if the state was previously none
            Interlocked.CompareExchange(ref _connectionStateValue, (int)ConnectionState.Negotiating, (int)ConnectionState.None);
        }
    }

    internal interface ISession
    {
        uint SessionId { get; }
        ushort? LocalId { get; }
    }
}
