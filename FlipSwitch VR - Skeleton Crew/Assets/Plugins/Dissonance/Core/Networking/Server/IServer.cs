using System;
using System.Collections.Generic;

namespace Dissonance.Networking.Server
{
    internal interface IServer<TPeer>
    {
        uint SessionId { get; }

        /// <summary>
        /// Send an unreliable network message
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        void SendUnreliable(TPeer connection, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable network message to a group of peers
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="packet"></param>
        void SendUnreliable(List<TPeer> connections, ArraySegment<byte> packet);

        /// <summary>
        /// Send a reliable network message
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="packet"></param>
        void SendReliable(TPeer connection, ArraySegment<byte> packet);

        /// <summary>
        /// Send a reliable network message to a group of peers
        /// </summary>
        /// <param name="connections"></param>
        /// <param name="packet"></param>
        void SendReliable(List<TPeer> connections, ArraySegment<byte> packet);

        /// <summary>
        /// Invoked when a new client has been added
        /// </summary>
        /// <param name="client"></param>
        void AddClient(ClientInfo<TPeer> client);
    }
}
