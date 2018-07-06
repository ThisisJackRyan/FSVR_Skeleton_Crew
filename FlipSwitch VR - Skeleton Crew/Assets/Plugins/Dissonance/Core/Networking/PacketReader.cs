using System;
using System.Collections.Generic;
using System.Text;
using Dissonance.Datastructures;

namespace Dissonance.Networking
{
    internal struct PacketReader
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(PacketReader).Name);

        #region fields and properties
        private readonly ArraySegment<byte> _array;
        private int _count;

        public ArraySegment<byte> Read
        {
            get { return new ArraySegment<byte>(_array.Array, _array.Offset, _count); }
        }

        public ArraySegment<byte> Unread
        {
            get { return new ArraySegment<byte>(_array.Array, _array.Offset + _count, _array.Count - _count); }
        }

        public ArraySegment<byte> All
        {
            get { return _array; }
        }
        #endregion

        #region constructor
        public PacketReader(ArraySegment<byte> array)
        {
            _array = array;
            _count = 0;
        }

        public PacketReader(byte[] array)
            : this(new ArraySegment<byte>(array))
        {
        }
        #endregion

        #region read primitive
        public PacketReader Skip(int countBytes)
        {
            Check(countBytes, "skipped bytes");
            _count += countBytes;

            return this;
        }

        private void Check(int count, string type)
        {
            if (_array.Count - count - _count < 0)
                throw Log.CreatePossibleBugException(string.Format("Insufficient space in packet reader to read {0}", type), "4AFBC61A-77D4-43B8-878F-796F0D921184");
        }

        private byte FastReadByte()
        {
            _count++;
            return _array.Array[_array.Offset + _count - 1];
        }

        public byte ReadByte()
        {
            Check(sizeof(byte), "byte");

            return FastReadByte();
        }

        public ushort ReadUInt16()
        {
            Check(sizeof(ushort), "ushort");

            var un = new Union16 {
                LSB = FastReadByte(),
                MSB = FastReadByte()
            };

            return un.UInt16;
        }

        public uint ReadUInt32()
        {
            Check(sizeof(uint), "uint");

            var un = new Union32();

            un.SetBytesFromNetworkOrder(
                FastReadByte(),
                FastReadByte(),
                FastReadByte(),
                FastReadByte()
            );

            return un.UInt32;
        }

        public ArraySegment<byte> ReadByteSegment()
        {
            //Read length prefix
            var length = ReadUInt16();

            //Now check that the rest of the data is available
            Check(length, "byte[]");

            //Get the segment from the middle of the buffer
            var segment = new ArraySegment<byte>(_array.Array, Unread.Offset, length);
            _count += length;

            return segment;
        }

        [CanBeNull]public string ReadString()
        {
            //Read the length prefix
            var length = ReadUInt16();

            //Special case for null
            if (length == 0)
                return null;
            else
                length--;

            //Now check that the rest of the string is available
            Check(length, "string");

            //Read the string
            var unread = Unread;
            var str = Encoding.UTF8.GetString(unread.Array, unread.Offset, length);

            //Apply the offset over the string length
            _count += length;

            return str;
        }

        public void SkipString()
        {
            var length = ReadUInt16();
            if (length != 0)
                length--;

            Check(length, "string");
            _count += length;
        }
        #endregion

        #region peek
        public PacketReader Peek(int offset = 0)
        {
            return new PacketReader(
                new ArraySegment<byte>(_array.Array, _array.Offset + _count + offset, _array.Count - _count - offset)
                );
        }
        #endregion

        #region read high level
        public bool ReadPacketHeader(out MessageTypes messageType)
        {
            var magic = ReadUInt16() == PacketWriter.Magic;

            if (magic)
                messageType = (MessageTypes)ReadByte();
            else
                messageType = default(MessageTypes);

            return magic;
        }

        public void ReadHandshakeRequest([CanBeNull] out string name)
        {
            name = ReadString();
        }

        public void ReadHandshakeResponseHeader(out uint session, out ushort clientId)
        {
            session = ReadUInt32();
            clientId = ReadUInt16();
        }

        /// <summary>
        /// Read the handshake response. Will totally overwrite the contents of outputRoomsToPeerId with a new state
        /// </summary>
        /// <param name="idMap"></param>
        /// <param name="outputRoomsToPeerId"></param>
        public void ReadHandshakeResponseBody(ClientIdCollection idMap, Dictionary<ushort, List<ushort>> outputRoomsToPeerId)
        {
            //Read ID map for all clients
            idMap.Deserialize(ref this);

            //Clear all the lists
            using (var enumerator = outputRoomsToPeerId.GetEnumerator())
                while (enumerator.MoveNext())
                    enumerator.Current.Value.Clear();

            //Read out lists per channel
            var channelCount = ReadUInt16();
            for (var i = 0; i < channelCount; i++)
            {
                var channel = ReadUInt16();
                var peerCount = ReadByte();

                //Get or create a list for this channel
                List<ushort> peers;
                if (!outputRoomsToPeerId.TryGetValue(channel, out peers))
                {
                    peers = new List<ushort>();
                    outputRoomsToPeerId[channel] = peers;
                }

                //Read out all the peers for this chanel
                for (var j = 0; j < peerCount; j++)
                    peers.Add(ReadUInt16());
            }
        }

        public void ReadhandshakeP2P(out ushort peerId)
        {
            //assigned peer ID
            peerId = ReadUInt16();
        }

        /// <summary>
        /// Read the state of a client, will get/create a ClientInfo object from the given dictionary
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public void ReadClientStateHeader(out string name, out ushort id)
        {
            // ReSharper disable once AssignNullToNotNullAttribute (Justification: we're sanity checking this immediately below)
            name = ReadString();
            if (name == null)
                throw Log.CreatePossibleBugException("Deserialized a ClientState packet with a null client Name", "5F77FC4F-4110-4A6F-8F96-97B393AD7324");

            id = ReadUInt16();
        }

        public void ReadClientStateRooms<TPeer>(ClientInfo<TPeer> info)
        {
            info.Rooms.Clear();

            var count = ReadUInt16();
            for (var i = 0; i < count; i++)
                info.Rooms.Add(ReadUInt16());
        }

        public void ReadRemoveClient(out ushort clientId)
        {
            clientId = ReadUInt16();
        }

        public void ReadVoicePacketHeader(out byte options, out ushort senderId, out ushort sequenceNumber, out ushort numChannels)
        {
            options = ReadByte();
            senderId = ReadUInt16();
            sequenceNumber = ReadUInt16();
            numChannels = ReadUInt16();
        }

        public void ReadVoicePacketChannel(out ushort bitfield, out ushort recipient)
        {
            bitfield = ReadUInt16();
            recipient = ReadUInt16();
        }

        public TextPacket ReadTextPacket(bool readText)
        {
            byte options = ReadByte();
            ushort senderId = ReadUInt16();
            ushort target = ReadUInt16();

            string txt = null;
            if (readText)
                txt = ReadString();
            else
                SkipString();

            return new TextPacket(senderId, (ChannelType)options, target, txt);
        }

        public uint ReadErrorWrongSession()
        {
            return ReadUInt32();
        }

        public void ReadRelay(List<ushort> destinations, out ArraySegment<byte> data)
        {
            //Read out destinations
            var count = ReadByte();
            for (var i = 0; i < count; i++)
                destinations.Add(ReadUInt16());

            //Read out data (not allocating a new array, it's just a slice of this packet)
            data = ReadByteSegment();
        }

        public void ReadDeltaChannelState(out bool joined, out ushort peer, out ushort room)
        {
            joined = ReadByte() != 0;
            peer = ReadUInt16();
            room = ReadUInt16();
        }
        #endregion
    }
}
