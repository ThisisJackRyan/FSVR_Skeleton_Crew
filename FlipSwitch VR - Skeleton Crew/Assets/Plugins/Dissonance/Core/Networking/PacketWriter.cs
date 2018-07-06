using System;
using System.Collections.Generic;
using System.Text;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking.Client;

namespace Dissonance.Networking
{
    /// <summary>
    /// Helper struct for writing a packet. This struct represents a position within a packet and may be copied to keep a reference to the same position
    /// </summary>
    internal struct PacketWriter
    {
        #region fields and properties
        internal const ushort Magic = 0x8bc7;

        private readonly ArraySegment<byte> _array;
        private int _count;

        /// <summary>
        /// A segment of all the bytes which have been written to so far
        /// </summary>
        public ArraySegment<byte> Written
        {
            get { return new ArraySegment<byte>(_array.Array, _array.Offset, _count); }
        }
        #endregion

        #region constructor
        /// <summary>
        /// Construct a new packet writer to write into the given array
        /// </summary>
        /// <param name="array">Array to write into (starting at index 0)</param>
        public PacketWriter(byte[] array)
            : this(new ArraySegment<byte>(array))
        {
        }

        /// <summary>
        /// Construct a new packet writer to write into the given array segment
        /// </summary>
        /// <param name="array">Segment to write into</param>
        public PacketWriter(ArraySegment<byte> array)
        {
            _array = array;
            _count = 0;
        }
        #endregion

        #region write primitive
        /// <summary>
        /// Write a single byte into the underlying array. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="b">value to write</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter Write(byte b)
        {
            _array.Array[_array.Offset + _count] = b;
            _count++;

            return this;
        }
        
        /// <summary>
        /// Write an unsigned 16 bit integer into the underlying array. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="u">value to write</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter Write(ushort u)
        {
            var un = new Union16 { UInt16 = u };
            Write(un.LSB);
            Write(un.MSB);

            return this;
        }

        /// <summary>
        /// Write an unsigned 32 bit integer into the underlying array. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="u">value to write</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter Write(uint u)
        {
            var un = new Union32 { UInt32 = u };

            byte b1, b2, b3, b4;
            un.GetBytesInNetworkOrder(out b1, out b2, out b3, out b4);

            Write(b1);
            Write(b2);
            Write(b3);
            Write(b4);

            return this;
        }

        /// <summary>
        /// Write a string into the underlying array. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="s">value to write (may be null)</param>
        /// <remarks>Should be read with PacketReader.ReadString()</remarks>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter Write([CanBeNull] string s)
        {
            //Special case for null input
            if (s == null)
            {
                Write((ushort)0);
                return this;
            }

            //Sanity check
            //This is not strictly accurate (we assume every char encodes to one byte, but it's a safe underestimate of string length)
            if (s.Length > ushort.MaxValue)
                throw new ArgumentException("Cannot encode strings with more than 65535 characters");

            //Write the UTF8 string out, leaving 2 bytes at the start for the length
            var length = Encoding.UTF8.GetBytes(s, 0, s.Length, _array.Array, _array.Offset + _count + sizeof(ushort));

            //This check is completely accurate (unlike the one above");
            if (length > ushort.MaxValue)
                throw new ArgumentException("Cannot encode strings which encode to more than 65535 UTF8 bytes");

            //Write out the length header, now we're satisfied everything fits
            Write((ushort)(length + 1));
            _count += length;

            return this;
        }

        /// <summary>
        /// Write some bytes into the underlying array. Mutate this writer to represent the position after the write. </summary>
        /// <param name="data">data to write</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter Write(ArraySegment<byte> data)
        {
            Write((ushort)data.Count);

            data.CopyTo(_array.Array, _array.Offset + _count);
            _count += data.Count;

            return this;
        }
        
        /// <summary>
        /// Write the constant magic number
        /// </summary>
        internal void WriteMagic()
        {
            Write((ushort)Magic);
        }
        #endregion

        #region write high level
        /// <summary>
        /// Write out a handshake request. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter WriteHandshakeRequest([NotNull] string name)
        {
            WriteMagic();
            Write((byte) MessageTypes.HandshakeRequest);

            Write(name);

            return this;
        }

        /// <summary>
        /// Write out a handshake response. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter WriteHandshakeResponse<TPeer>(uint session, ushort clientId, ClientIdCollection idMap, Dictionary<ushort, List<ClientInfo<TPeer>>> peersByRoom)
        {
            WriteMagic();
            Write((byte)MessageTypes.HandshakeResponse);
            Write(session);

            //Assigned ID for this client
            Write(clientId);

            //Write ID map for all clients
            idMap.Serialize(ref this);

            //Channel count
            Write((ushort)peersByRoom.Count);

            //Write out list of peers in each channel (ugly enumerator instead of foreach because this doesn't box)
            using (var enumerator = peersByRoom.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var channel = enumerator.Current.Key;
                    var peers = enumerator.Current.Value;

                    Write(channel);

                    //Write length prefixed list of peer IDs
                    Write((byte)peers.Count);
                    for (var i = 0; i < peers.Count; i++)
                        Write(peers[i].PlayerId);
                }
            }

            return this;
        }

        /// <summary>
        /// Write out a handshake response. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter WriteHandshakeP2P(uint session, ushort peerId)
        {
            WriteMagic();
            Write((byte)MessageTypes.HandshakeP2P);
            Write(session);

            //Assigned ID for this new peer
            Write(peerId);

            return this;
        }

        /// <summary>
        /// Write out a client state packet. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="clientId"></param>
        /// <param name="rooms">Rooms state of the local player</param>
        /// <param name="name">Name of the local player</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        public PacketWriter WriteClientState(uint session, string name, ushort clientId, Rooms rooms)
        {
            if (name == null)
                throw new ArgumentNullException("name", "Attempted to serialize ClientState with a null name");

            WriteMagic();
            Write((byte)MessageTypes.ClientState);
            Write(session);

            //Write out ID of this client
            Write(name);
            Write(clientId);

            //Write out the rooms this client is listening to
            Write((ushort)rooms.Count);
            for (var i = 0; i < rooms.Count; i++)
                Write(rooms[i]);

            return this;
        }

        public PacketWriter WriteRemoveClient(uint session, ushort clientId)
        {
            WriteMagic();
            Write((byte)MessageTypes.RemoveClient);
            Write(session);

            Write(clientId);

            return this;
        }

        /// <summary>
        /// Write out a voice packet. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="senderId">Local player ID</param>
        /// <param name="sequenceNumber">Sequence number (monotonically increases with audio frames)</param>
        /// <param name="channelSession"></param>
        /// <param name="channels">List of local open channels</param>
        /// <param name="encodedAudio">The encoded audio data</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        internal PacketWriter WriteVoiceData(uint session, ushort senderId, ref ushort sequenceNumber, byte channelSession, IList<OpenChannel> channels, ArraySegment<byte> encodedAudio)
        {
            WriteMagic();
            Write((byte)MessageTypes.VoiceData);
            Write(session);

            //Write out 8 bits of metadata (e.g. quality, codec)
            // 0 0 - Unused
            // 0 0 - Unused
            // 0 0 - Unused
            // 0 0 - channel Session number (as a 2 bit number)
            Write((byte)(
                0 << 6 |
                0 << 4 |
                0 << 2 |
                (channelSession % 4)
            ));

            Write(senderId);
            Write(unchecked(sequenceNumber++));

            //Write out a list of channels this packet is for
            Write((ushort)channels.Count);
            for (var i = 0; i < channels.Count; i++)
            {
                var channel = channels[i];

                Write(channel.Bitfield);
                Write(channel.Recipient);
            }

            //Write out the encoded audio
            Write(encodedAudio);

            return this;
        }

        /// <summary>
        /// Write out a text message packet. Mutate this writer to represent the position after the write.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="senderId">Local player ID</param>
        /// <param name="recipient">Type of recipinent</param>
        /// <param name="target">ID of the recipient (deepnds room or player ID, depends upon recipinent parameter)</param>
        /// <param name="data">Message to send</param>
        /// <returns>A copy of this writer (after the write has been applied)</returns>
        internal PacketWriter WriteTextPacket(uint session, ushort senderId, ChannelType recipient, ushort target, string data)
        {
            WriteMagic();
            Write((byte)MessageTypes.TextData);
            Write(session);

            Write((byte)recipient);
            Write((ushort)senderId);
            Write((ushort)target);
            Write(data);

            return this;
        }

        public PacketWriter WriteErrorWrongSession(uint session)
        {
            WriteMagic();
            Write((byte)MessageTypes.ErrorWrongSession);
            Write(session);

            return this;
        }

        public PacketWriter WriteRelay<TPeer>(uint session, List<ClientInfo<TPeer>> destinations, ArraySegment<byte> segment, bool reliable)
        {
            //Write header
            WriteMagic();
            Write((byte)(reliable ? MessageTypes.ServerRelayReliable : MessageTypes.ServerRelayUnreliable));
            Write(session);

            //Write out destination list (remove each peer we send to from the list. Peers we don't know about will remain in the list)
            Write((byte)destinations.Count);
            for (var i = 0; i < destinations.Count; i++)
                Write(destinations[i].PlayerId);

            //Write out the actual data
            Write(segment);

            return this;
        }

        public PacketWriter WriteDeltaChannelState(uint session, bool joined, ushort peer, ushort room)
        {
            //Write header
            WriteMagic();
            Write((byte)MessageTypes.DeltaChannelState);
            Write(session);

            //Write the change
            Write((byte)(joined ? 1 : 0));
            Write(peer);
            Write(room);

            return this;
        }
        #endregion
    }
}
