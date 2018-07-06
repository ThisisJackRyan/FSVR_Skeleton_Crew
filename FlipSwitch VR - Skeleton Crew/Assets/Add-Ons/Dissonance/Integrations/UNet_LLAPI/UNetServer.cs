using System;
using Dissonance.Networking;
using UnityEngine.Networking;

namespace Dissonance.Integrations.UNet_LLAPI
{
    public class UNetServer
        : BaseServer<UNetServer, UNetClient, int>
    {
        private readonly UNetCommsNetwork _network;
        private readonly ServerConnectionDetails _connection;

        private readonly byte[] _receiveBuffer = new byte[1024];

        private bool _socketValid;
        private int _socket = -1;

        public UNetServer(UNetCommsNetwork network, ServerConnectionDetails connection)
        {
            _network = network;
            _connection = connection;
        }

        public override void Connect()
        {
            base.Connect();

            _socket = NetworkTransport.AddHost(_network.Topology, _connection.Port);
            if (_socket == -1)
                throw new DissonanceException(string.Format("Failed to create Dissonance server on port '{0}', (port may already be in use)", _connection.Port));
            
            _socketValid = true;
        }

        public override void Disconnect()
        {
            base.Disconnect();

            if (_socketValid)
            {
                NetworkTransport.RemoveHost(_socket);
                _socketValid = false;
            }
        }

        protected override void ReadMessages()
        {
            if (!_socketValid)
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

                eventType = NetworkTransport.ReceiveFromHost(_socket, out senderConnectionId, out channelId, _receiveBuffer, _receiveBuffer.Length, out dataSize, out error);

                if (error != 0)
                {
                    if (UNetCommsNetwork.FatalError((NetworkError)error, Log))
                        ClientDisconnected(senderConnectionId);

                    Log.Error("UNet Error: {0}", (NetworkError)error);
                }
                else
                {
                    switch (eventType)
                    {
                        case NetworkEventType.DataEvent:
                            NetworkReceivedPacket(senderConnectionId, new ArraySegment<byte>(_receiveBuffer, 0, dataSize));
                            break;

                        case NetworkEventType.DisconnectEvent:
                            ClientDisconnected(senderConnectionId);
                            break;

                        case NetworkEventType.ConnectEvent:
                        case NetworkEventType.Nothing:
                        case NetworkEventType.BroadcastEvent:
                            break;

                        default:
                            Log.Error("Dissonance UNet received unknown event: '{0}'", eventType);
                            break;
                    }
                }

            } while (eventType != NetworkEventType.Nothing);
        }

        private void Send(int connection, int channel, ArraySegment<byte> packet)
        {
            if (!_socketValid)
            {
                Log.Warn("Attempted to send message to an invalid socket");
                return;
            }

            UNetCommsNetwork.Send(_socket, connection, channel, packet, Log);
        }

        protected override void SendReliable(int connection, ArraySegment<byte> packet)
        {
            Send(connection, _network.SystemMessagesChannel, packet);
        }

        protected override void SendUnreliable(int connection, ArraySegment<byte> packet)
        {
            Send(connection, _network.VoiceDataChannel, packet);
        }
    }
}
