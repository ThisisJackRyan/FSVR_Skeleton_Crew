using System;
using Dissonance.Networking;
using Dissonance.Networking.Client;
using UnityEngine.Networking;

namespace Dissonance.Integrations.UNet_HLAPI
{
    public class HlapiClient
        : BaseClient<HlapiServer, HlapiClient, HlapiConn>
    {
        #region fields and properties
        private readonly HlapiCommsNetwork _network;

        private readonly NetworkWriter _sendWriter;

        private readonly byte[] _receiveBuffer = new byte[1024];

        private bool _fatalError;
        #endregion

        #region constructors
        public HlapiClient(HlapiCommsNetwork network)
            : base(network)
        {
            _network = network;

            _sendWriter = new NetworkWriter(new byte[1024]);


        }
        #endregion

        #region connect/disconnect
        public override void Connect()
        {
            //we handle loopback explicitly, so if the server is locally hosted we don't need to register the network handler
            //This is important because otherwise we'd overwrite the server message handler!
            if (!_network.Mode.IsServerEnabled())
                NetworkManager.singleton.client.RegisterHandler(_network.TypeCode, OnMessageReceivedHandler);

            Connected();
        }

        public override void Disconnect()
        {
            // Bind a handler to discard all Dissonance messages to the local client (if the server is not handling them).
            // Don't bother is client isn't null, because by definition we can't receive any messages then!
            if (!_network.Mode.IsServerEnabled() && NetworkManager.singleton.client != null)
                NetworkManager.singleton.client.RegisterHandler(_network.TypeCode, HlapiCommsNetwork.NullMessageReceivedHandler);

            base.Disconnect();
        }
        #endregion

        public override ClientStatus Update()
        {
            if (_fatalError)
                return ClientStatus.Error;

            return base.Update();
        }

        #region send/receive
        private void OnMessageReceivedHandler(NetworkMessage netMsg)
        {
            NetworkReceivedPacket(_network.CopyToArraySegment(netMsg.reader, new ArraySegment<byte>(_receiveBuffer)));
        }

        protected override void ReadMessages()
        {
            //Messages are received in an event handler, so we don't need to do any work to read events
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            if (!Send(packet, _network.ReliableSequencedChannel))
            {
                Log.Error("Failed to send reliable packet (unknown HLAPI error)");
                _fatalError = true;
            }
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            Send(packet, _network.UnreliableChannel);
        }

        private bool Send(ArraySegment<byte> packet, byte channel)
        {
            if (_network.PreprocessPacketToServer(packet))
                return true;

            var length = _network.CopyPacketToNetworkWriter(packet, _sendWriter);

            if (!NetworkManager.singleton.client.connection.SendBytes(_sendWriter.AsArray(), length, channel))
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
