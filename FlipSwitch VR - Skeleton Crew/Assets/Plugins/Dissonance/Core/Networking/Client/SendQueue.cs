using System;
using System.Collections.Generic;
using Dissonance.Datastructures;

namespace Dissonance.Networking.Client
{
    internal class SendQueue<TPeer>
        : ISendQueue<TPeer>
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(SendQueue<TPeer>).Name);

        private readonly IClient<TPeer> _client;

        private readonly List<ArraySegment<byte>> _serverReliableQueue = new List<ArraySegment<byte>>();
        private readonly List<ArraySegment<byte>> _serverUnreliableQueue = new List<ArraySegment<byte>>();
        private readonly List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>> _reliableP2PQueue = new List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>();
        private readonly List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>> _unreliableP2PQueue = new List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>>();

        private readonly ConcurrentPool<byte[]> _sendBufferPool;
        public ConcurrentPool<byte[]> SendBufferPool
        {
            get { return _sendBufferPool; }
        }

        private readonly Pool<List<ClientInfo<TPeer?>>> _listPool = new Pool<List<ClientInfo<TPeer?>>>(32, () => new List<ClientInfo<TPeer?>>());
        #endregion

        #region constructor
        public SendQueue(IClient<TPeer> client, ConcurrentPool<byte[]> bytePool)
        {
            _client = client;
            _sendBufferPool = bytePool;
        }
        #endregion

        public void Update()
        {
            //Reliable traffic to server
            for (var i = 0; i < _serverReliableQueue.Count; i++)
            {
                var item = _serverReliableQueue[i];
                _client.SendReliable(item);
                Recycle(item.Array);
            }
            _serverReliableQueue.Clear();

            //Unreliable traffic to server
            for (var i = 0; i < _serverUnreliableQueue.Count; i++)
            {
                var item = _serverUnreliableQueue[i];
                _client.SendUnreliable(item);
                Recycle(item.Array);
            }
            _serverUnreliableQueue.Clear();

            //P2P reliable traffic
            for (var i = 0; i < _reliableP2PQueue.Count; i++)
            {
                var item = _reliableP2PQueue[i];

                //Send it
                _client.SendReliableP2P(item.Key, item.Value);

                //Recycle
                Recycle(item.Value.Array);
                item.Key.Clear();
                _listPool.Put(item.Key);
            }
            _reliableP2PQueue.Clear();

            //P2P reliable traffic
            for (var i = 0; i < _unreliableP2PQueue.Count; i++)
            {
                var item = _unreliableP2PQueue[i];

                //Send it
                _client.SendUnreliableP2P(item.Key, item.Value);

                //Recycle
                Recycle(item.Value.Array);
                item.Key.Clear();
                _listPool.Put(item.Key);
            }
            _unreliableP2PQueue.Clear();
        }

        private void Recycle(byte[] array)
        {
            _sendBufferPool.Put(array);
        }

        public void Stop()
        {
            var dropped = _serverReliableQueue.Count
                        + _serverUnreliableQueue.Count
                        + _reliableP2PQueue.Count
                        + _unreliableP2PQueue.Count;

            Log.Debug("Stopped network SendQueue (dropping {0} remaining packets)", dropped);

            _serverReliableQueue.Clear();
            _serverUnreliableQueue.Clear();
            _reliableP2PQueue.Clear();
            _unreliableP2PQueue.Clear();
        }

        #region Enqueue
        /// <summary>
        /// Send a reliable message to the server
        /// </summary>
        public void EnqueueReliable(ArraySegment<byte> packet)
        {
            _serverReliableQueue.Add(packet);
        }

        /// <summary>
        /// Send an unreliable message to the server
        /// </summary>
        public void EnqeueUnreliable(ArraySegment<byte> packet)
        {
            _serverUnreliableQueue.Add(packet);
        }

        public void EnqueueReliableP2P(ushort localId, IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            EnqueueP2P(
                localId,
                destinations,
                _reliableP2PQueue,
                packet
            );
        }

        public void EnqueueUnreliableP2P(ushort localId, IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet)
        {
            EnqueueP2P(localId, destinations, _unreliableP2PQueue, packet);
        }

        private void EnqueueP2P(ushort localId, ICollection<ClientInfo<TPeer?>> destinations, List<KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>> queue, ArraySegment<byte> packet)
        {
            //early exit
            if (destinations.Count == 0)
                return;

            //Copy destinations into a new list we're allowed to mutate
            var dests = _listPool.Get();
            dests.Clear();
            dests.AddRange(destinations);

            //Make sure we don't send to ourselves
            for (var i = 0; i < dests.Count; i++)
            {
                if (dests[i].PlayerId == localId)
                {
                    dests.RemoveAt(i);
                    break;
                }
            }

            //If we were only trying to send to ourself we can early exit now
            if (dests.Count == 0)
                return;

            //Add to queue to send next update
            queue.Add(new KeyValuePair<List<ClientInfo<TPeer?>>, ArraySegment<byte>>(dests, packet));
        }
        #endregion
    }

    internal interface ISendQueue<TPeer>
        where TPeer : struct
    {
        [NotNull] ConcurrentPool<byte[]> SendBufferPool { get; }

        /// <summary>
        /// Send a reliable message to the server
        /// </summary>
        void EnqueueReliable(ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable message to the server
        /// </summary>
        void EnqeueUnreliable(ArraySegment<byte> packet);

        /// <summary>
        /// Send a reliable message directly to the given list of peers (excluding the local peer)
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        void EnqueueReliableP2P(ushort localId, [NotNull] IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);

        /// <summary>
        /// Send an unreliable message directly to the given list of peers (excluding the local peer)
        /// </summary>
        /// <param name="localId"></param>
        /// <param name="destinations"></param>
        /// <param name="packet"></param>
        void EnqueueUnreliableP2P(ushort localId, [NotNull] IList<ClientInfo<TPeer?>> destinations, ArraySegment<byte> packet);
    }
}
