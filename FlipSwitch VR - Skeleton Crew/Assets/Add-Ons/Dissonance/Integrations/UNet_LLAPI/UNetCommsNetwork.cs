using System;
using Dissonance.Networking;
using UnityEngine;
using UnityEngine.Networking;
using JetBrains.Annotations; //todo this was added as a fix, may need removed

namespace Dissonance.Integrations.UNet_LLAPI
{
    public struct ClientConnectionDetails
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }

    public struct ServerConnectionDetails
    {
        public int Port { get; set; }
    }

    public class UNetCommsNetwork
        : BaseCommsNetwork<UNetServer, UNetClient, int, ClientConnectionDetails, ServerConnectionDetails>
    {
        public string ServerAddress { get; private set; }

        [SerializeField]private int _maxConnections = 64;
        public int MaxConnections
        {
            get { return _maxConnections; }
        }

        private readonly int _voiceChannel;
        public int VoiceDataChannel
        {
            get { return _voiceChannel; }
        }

        private readonly int _sysChannel;
        public int SystemMessagesChannel
        {
            get { return _sysChannel; }
        }

        [SerializeField]private int _port = 5889;
        public ushort Port
        {
            get { return (ushort)_port; }
            set
            {
                if (Status != ConnectionStatus.Disconnected)
                    Log.Warn("Port changed while network is active. The network must be restarted for this change to be applied.");

                _port = value;
            }
        }

        private readonly HostTopology _topology;
        [NotNull] internal HostTopology Topology
        {
            get { return _topology; }
        }

        // ReSharper disable once FieldCanBeMadeReadOnly.Local (Justification: Used by editor)
        // ReSharper disable once ConvertToConstant.Local (Justification: Used by editor)
        [SerializeField]private bool _disableNetworkLifetimeManagement;

        public UNetCommsNetwork()
        {
            var config = new ConnectionConfig();
            _voiceChannel = config.AddChannel(QosType.Unreliable);
            _sysChannel = config.AddChannel(QosType.ReliableSequenced);
            _topology = new HostTopology(config, MaxConnections);
        }

        protected override UNetServer CreateServer(ServerConnectionDetails details)
        {
            return new UNetServer(this, details);
        }

        protected override UNetClient CreateClient(ClientConnectionDetails details)
        {
            return new UNetClient(this, details);
        }

        protected override void Initialize()
        {
            if (!_disableNetworkLifetimeManagement)
                NetworkTransport.Init();
        }

        private void OnDestroy()
        {
            if (!_disableNetworkLifetimeManagement)
                NetworkTransport.Shutdown();
        }

        public void InitializeAsDedicatedServer()
        {
            RunAsDedicatedServer(new ServerConnectionDetails {
                Port = _port
            });
        }

        public void InitializeAsServer()
        {
            ServerAddress = "127.0.0.1";

            RunAsHost(
                new ServerConnectionDetails {
                    Port = _port
                },
                new ClientConnectionDetails {
                    Address = ServerAddress,
                    Port = _port
                }
            );
        }

        public void InitializeAsClient(string serverAddress)
        {
            // UNet doesn't like "localhost"
            if (serverAddress == "localhost")
                serverAddress = "127.0.0.1";

            ServerAddress = serverAddress;

            RunAsClient(new ClientConnectionDetails {
                Address = ServerAddress,
                Port = _port
            });
        }

        internal static bool Send(int socket, int connection, int channel, ArraySegment<byte> packet, Log log)
        {
            if (packet.Offset != 0)
                throw new ArgumentException("non-zero packet offset");

            byte error;
            var success = NetworkTransport.Send(socket, connection, channel, packet.Array, packet.Count, out error);

            if (!success)
            {
                log.Error("Error sending voice data: {0}", (NetworkError)error);

                if (FatalError((NetworkError)error, log))
                    return false;
            }

            return true;
        }

        internal static bool FatalError(NetworkError error, Log log)
        {
            switch (error)
            {
                case NetworkError.WrongHost:
                case NetworkError.WrongConnection:
                case NetworkError.WrongChannel:
                case NetworkError.NoResources:
                case NetworkError.Timeout:
                case NetworkError.VersionMismatch:
                case NetworkError.DNSFailure:
                case NetworkError.CRCMismatch:
                case NetworkError.BadMessage:
                    return true;

                case NetworkError.Ok:
                case NetworkError.MessageToLong:
                case NetworkError.WrongOperation:
                    return false;

                default:
                    log.Error("Dissonance UNet received unknown NetworkError: '{0}'", error);
                    return true;
            }
        }
    }
}
