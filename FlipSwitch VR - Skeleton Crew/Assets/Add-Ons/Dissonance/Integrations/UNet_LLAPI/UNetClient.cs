using System;
using Dissonance.Networking;
using Dissonance.Networking.Client;
using UnityEngine.Networking;

namespace Dissonance.Integrations.UNet_LLAPI
{
    public class UNetClient
        : BaseClient<UNetServer, UNetClient, int>
    {
        private readonly UNetCommsNetwork _network;
        private readonly ClientConnectionDetails _server;

        private readonly byte[] _readBuffer = new byte[1024];

        private bool _error;
        private bool _connectionEstablished;
        private int _socket = -1;
        private int _connection = -1;

        public UNetClient(UNetCommsNetwork network, ClientConnectionDetails server)
            : base(network)
        {
            _network = network;
            _server = server;
        }

        public override void Connect()
        {
            byte error;
            _socket = NetworkTransport.AddHost(_network.Topology);
            _connection = NetworkTransport.Connect(_socket, _server.Address, _server.Port, 0, out error);

            if (error == (int) NetworkError.Ok)
                _connectionEstablished = true;
            else
            {
                Log.Error("Failed to connect to Dissonance server on port {0}, Error {1}", _server.Port, (NetworkError) error);
                _error = true;
            }
        }

        public override void Disconnect()
        {
            base.Disconnect();

            if (_connectionEstablished)
            {
                byte error;
                NetworkTransport.Disconnect(_socket, _connection, out error);
                if (error != (int)NetworkError.Ok)
                    Log.Error("Failed to cleanly disconnect from Dissonance server at {0}:{1}, Error {2}", _server.Address, _server.Port, (NetworkError)error);

                NetworkTransport.RemoveHost(_socket);

                _connectionEstablished = false;
            }
        }

        public override ClientStatus Update()
        {
            if (_error)
                return ClientStatus.Error;

            return base.Update();
        }

        protected override void ReadMessages()
        {
            if (!_connectionEstablished)
            {
                Log.Warn("Attempted to read messages from an invalid socket");
                return;
            }

            NetworkEventType eventType;

            do
            {
                int senderConnectionId;
                int channelId;
                int dataSize;
                byte error;

                eventType = NetworkTransport.ReceiveFromHost(_socket, out senderConnectionId, out channelId, _readBuffer, _readBuffer.Length, out dataSize, out error);

                if (error != 0)
                {
                    Log.Error("Error reading client socket: {0}", (NetworkError)error);

                    if (UNetCommsNetwork.FatalError((NetworkError)error, Log))
                    {
                        _error = true;
                        return;
                    }
                }
                else
                {
                    switch (eventType)
                    {
                        case NetworkEventType.DataEvent:
                            NetworkReceivedPacket(new ArraySegment<byte>(_readBuffer, 0, dataSize));
                            break;

                        case NetworkEventType.ConnectEvent:
                            Connected();
                            break;

                        case NetworkEventType.DisconnectEvent:
                            _error = true;
                            break;

                        case NetworkEventType.Nothing:
                        case NetworkEventType.BroadcastEvent:
                            break;

                        default:
                            Log.Error("Received unknown network event '{0}'", eventType);
                            break;
                    }
                }
            } while (eventType != NetworkEventType.Nothing);
        }

        private void Send(int channel, ArraySegment<byte> packet)
        {
            if (!_connectionEstablished)
            {
                Log.Warn("Attempted to send message to an invalid socket");
                return;
            }

            if (!UNetCommsNetwork.Send(_socket, _connection, channel, packet, Log))
                _error = true;
        }

        protected override void SendReliable(ArraySegment<byte> packet)
        {
            Send(_network.SystemMessagesChannel, packet);
        }

        protected override void SendUnreliable(ArraySegment<byte> packet)
        {
            Send(_network.VoiceDataChannel, packet);
        }
    }
}
