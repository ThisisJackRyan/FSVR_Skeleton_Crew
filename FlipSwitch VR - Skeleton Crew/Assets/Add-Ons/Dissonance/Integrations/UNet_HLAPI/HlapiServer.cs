using System;
using System.Collections.Generic;
using Dissonance.Networking;
using Dissonance.Networking.Server;
using UnityEngine;
using UnityEngine.Networking;

namespace Dissonance.Integrations.UNet_HLAPI
{
    public class HlapiServer
        : BaseServer<HlapiServer, HlapiClient, HlapiConn>
    {
        #region fields and properties
        private readonly HlapiCommsNetwork _network;

        private readonly NetworkWriter _sendWriter;

        private readonly byte[] _receiveBuffer = new byte[1024];

        private bool _fatalError;

        private readonly List<NetworkConnection> _addedConnections = new List<NetworkConnection>();
        #endregion

        #region constructors
        public HlapiServer(HlapiCommsNetwork network)
        {
            _network = network;

            _sendWriter = new NetworkWriter(new byte[1024]);
        }
        #endregion

        public override void Connect()
        {
            NetworkServer.RegisterHandler(_network.TypeCode, OnMessageReceivedHandler);

            base.Connect();
        }

        private void OnMessageReceivedHandler(NetworkMessage netmsg)
        {
            NetworkReceivedPacket(new HlapiConn(netmsg.conn), _network.CopyToArraySegment(netmsg.reader, new ArraySegment<byte>(_receiveBuffer)));
        }

        protected override void AddClient(ClientInfo<HlapiConn> client)
        {
            base.AddClient(client);

            if (client.PlayerName != _network.PlayerName)
                _addedConnections.Add(client.Connection.Connection);
        }

        public override void Disconnect()
        {
            base.Disconnect();
            NetworkServer.RegisterHandler(_network.TypeCode, HlapiCommsNetwork.NullMessageReceivedHandler);
        }

        protected override void ReadMessages()
        {
            //Messages are received in an event handler, so we don't need to do any work to read events
        }

        public override ServerState Update()
        {
            if (_fatalError)
                return ServerState.Error;
            //Poll for disconnections
            for (var i = _addedConnections.Count - 1; i >= 0; i--)
            {
                if (!NetworkServer.connections.Contains(_addedConnections[i]))
                {
                    Debug.LogError("Disconnected in HLAPI");

                    ClientDisconnected(new HlapiConn(_addedConnections[i]));
                    _addedConnections.RemoveAt(i);
                }
            }

            return base.Update();
        }

        protected override void SendReliable(HlapiConn connection, ArraySegment<byte> packet)
        {
            if (!Send(packet, connection, _network.ReliableSequencedChannel))
            {
                Log.Error("Failed to send reliable packet (unknown HLAPI error)");
                _fatalError = true;
            }
        }

        protected override void SendUnreliable(HlapiConn connection, ArraySegment<byte> packet)
        {
            Send(packet, connection, _network.UnreliableChannel);
        }

        private bool Send(ArraySegment<byte> packet, HlapiConn connection, byte channel)
        {
            if (_network.PreprocessPacketToClient(packet, connection))
                return true;

            var length = _network.CopyPacketToNetworkWriter(packet, _sendWriter);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse (Justification it shouldn't be null, but sanity check anyway)
            // ReSharper disable HeuristicUnreachableCode
            if (connection.Connection == null)
            {
                Log.Error("Cannot send to a null destination");
                return false;
            }
            // ReSharper restore HeuristicUnreachableCode
            else if (!connection.Connection.SendBytes(_sendWriter.AsArray(), length, channel))
            {
                return false;
            }

            return true;
        }
    }
}
