using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Extensions;

namespace Dissonance.Networking.Client
{
    internal class VoiceReceiver<TPeer>
        where TPeer : struct
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(VoiceReceiver<TPeer>).Name);
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(0.6);

        private readonly ISession _session;
        private readonly IClientCollection<TPeer?> _clients;
        private readonly EventQueue _events;
        private readonly Rooms _rooms;
        private readonly ConcurrentPool<byte[]> _pool;

        [ItemCanBeNull] private readonly List<ReceivingState> _receiving = new List<ReceivingState>();

        private readonly List<int> _tmpCompositeIdBuffer = new List<int>();
        #endregion

        #region constructor
        public VoiceReceiver(ISession session, IClientCollection<TPeer?> clients, EventQueue events, Rooms rooms, ConcurrentPool<byte[]> pool)
        {
            _session = session;
            _clients = clients;
            _events = events;
            _rooms = rooms;
            _pool = pool;

            _events.OnEnqueuePlayerLeft += OnPlayerLeft;
        }
        #endregion

        private void OnPlayerLeft(string name)
        {
            for (var i = 0; i < _receiving.Count; i++)
            {
                var r = _receiving[i];
                if (r != null && r.Peer.PlayerName == name)
                {
                    if (r.Open)
                        r.StopSpeaking();
                    _receiving[i] = null;
                    return;
                }
            }
        }

        public void Stop()
        {
            //Stop all incoming voice streams
            for (var i = 0; i < _receiving.Count; i++)
            {
                var r = _receiving[i];
                if (r != null && _receiving[i].Open)
                    _receiving[i].StopSpeaking();
            }

            //Discard all receivers
            _receiving.Clear();
        }

        public void Update()
        {
            CheckTimeouts();
        }

        /// <summary>
        /// Transition to a non-receiving state for all receivers which have not received any packets within a short window
        /// </summary>
        private void CheckTimeouts()
        {
            var now = DateTime.Now;

            for (var i = _receiving.Count - 1; i >= 0; i--)
            {
                var r = _receiving[i];
                if (r != null)
                    r.CheckTimeout(now, Timeout, _events);
            }
        }

        public void ReceiveVoiceData(ref PacketReader reader)
        {
            //Read header from voice packet
            byte options;
            ushort senderId, sequenceNumber, numChannels;
            reader.ReadVoicePacketHeader(out options, out senderId, out sequenceNumber, out numChannels);

            var channelSession = options & 3;

            //Early exit if sender peer doesn't exist
            var receiver = GetOrCreateState(senderId);
            if (receiver == null)
            {
                Log.Debug("Received voice packet from unknown peer '{0}'", senderId);
                return;
            }

            //Early exit if sender peer has left the session
            if (!receiver.Peer.IsConnected)
            {
                Log.Debug("Received a voice packet for a disconnected peer '{0}'", receiver.Peer.PlayerName);
                return;
            }

            //Read the list of channels this voice data is being broadcast on and accumulate info about the channels we're listening on
            bool positional, allClosing, forceReset;
            float ampMult;
            ChannelPriority priority;
            ReadChannelStates(ref reader, receiver, numChannels, out positional, out allClosing, out forceReset, out priority, out ampMult);

            //Update the statistics for the channel this data is coming in over
            var discardVoice = !UpdateSpeakerStates(receiver, allClosing, forceReset, channelSession, sequenceNumber);

            //Send voice data onwards
            if (!discardVoice)
            {
                //Copy voice data into another buffer (we can't keep hold of this one, it will be recycled after we finish processing this packet)
                var frame = reader.ReadByteSegment().CopyTo(_pool.Get());

                //Send the event (buffer will be recycled after the event has been dispatched)
                _events.EnqueueVoiceData(new VoicePacket(receiver.Peer.PlayerName, priority, ampMult, positional, frame, receiver.SequenceNumber));
            }

            //If necessary stop speaking
            if (receiver.Open && allClosing)
                receiver.StopSpeaking();
        }

        private static bool UpdateSpeakerStates([NotNull] ReceivingState state, bool allClosing, bool forceReset, int channelSession, ushort sequenceNumber)
        {
            //If we need to reset the playback system stop speech right now, it will be restarted if necessary
            if ((forceReset || state.CurrentChannelSession != channelSession) && state.Open)
            {
                if (forceReset)
                {
                    Log.Trace("Channel reset due to forced reset");
                }
                else if (state.CurrentChannelSession != channelSession)
                {
                    Log.Trace(state.Open
                                  ? "Channel Session has changed: {0} => {1} (Triggering forced playback reset)"
                                  : "Channel Session has changed: {0} => {1}",
                              state.CurrentChannelSession,
                              channelSession);
                }

                state.StopSpeaking();
            }

            //If necessary start speaking (don't bother if allClosing, because that would create a 1 packet session, not much point to that)
            if (!allClosing && !state.Open)
            {
                //Try to start speaking and discard packet if we fail for some reason
                if (!state.TryStartSpeaking(sequenceNumber, channelSession))
                    return false;
            }

            //If we're now in a speech session update it
            if (state.Open)
            {
                //Update the sequence number (discard packet if we fail for some reason)
                if (!state.UpdateSequenceNumber(sequenceNumber))
                    return false;                
            }

            return state.Open;
        }

        #region channels
        /// <summary>
        /// Read all the channel data from the packet and accumulate data about the channels we are listening to
        /// </summary>
        /// <param name="reader">Packetreader to read data from</param>
        /// <param name="state">The receiving state we're receiving this from</param>
        /// <param name="numChannels">The number of channels in the packet reader</param>
        /// <param name="positional">Indicates if we should play the audio in this packet positionally</param>
        /// <param name="allClosing">Indicates if all channels are closing (i.e. we should stop speech after this packet)</param>
        /// <param name="forceReset">Indicates if a reset of the playback system should be forced (i.e. stop and immediately start speaking)</param>
        /// <param name="priority">Priority to play back this speech with</param>
        /// <param name="ampMult">Amplitude to play this speech with</param>
        private void ReadChannelStates(ref PacketReader reader, [NotNull] ReceivingState state, ushort numChannels, out bool positional, out bool allClosing, out bool forceReset, out ChannelPriority priority, out float ampMult)
        {
            //Accumulate aggregate information about all the channels
            positional = true;
            allClosing = true;
            ampMult = 0;
            var allResetting = true;
            priority = ChannelPriority.None;

            for (var i = 0; i < numChannels; i++)
            {
                //Parse a channel of information from the header
                ushort channelBitfield;
                ushort channelRecipient;
                reader.ReadVoicePacketChannel(out channelBitfield, out channelRecipient);
                var channel = new ChannelBitField(channelBitfield);

                //Skip onwards if we don't care about this channel
                if (!ChannelAddressesUs(channel, channelRecipient))
                    continue;

                //Accumulate aggregate metadata over all channels...
                // - Positional playback if *all* channels are positional
                // - Closing (i.e. end playback) if *all* are closing
                // - Amplitude is max amplitude of channels
                // - Priority is max priority of channels
                positional &= channel.IsPositional;
                allClosing &= channel.IsClosing;
                ampMult = Math.Max(ampMult, channel.AmplitudeMultiplier);
                priority = (ChannelPriority)Math.Max((int)priority, (int)channel.Priority);

                //Form a unique ID for this channel so we can keep track of it across packets
                var compositeId = ((int)channel.Type | (channelRecipient << 8));
                _tmpCompositeIdBuffer.Add(compositeId);
                
                // Check if this channel is the same channel as we think it is (i.e. if it's been closed and re-opened in between packets).
                // We can tell by checking if the channel session has changed (2 bit number packed into the header, increments every time a channel closes).
                if (!state.CheckChannelSession(compositeId, channel.SessionId))
                    allResetting = false;
            }

            //Reset local playback for this peer if *all* channels are resetting (but only if we're speaking - a reset makes no sense otherwise)
            forceReset = allResetting && state.Open;

            //Remove all channels from this peer which we no longer care about
            state.ClearChannels(_tmpCompositeIdBuffer);
            _tmpCompositeIdBuffer.Clear();
        }

        private bool ChannelAddressesUs(ChannelBitField channel, ushort recipient)
        {
            //Drop all incoming voice before we've been assigned a network ID
            if (!_session.LocalId.HasValue)
                return false;

            if (channel.Type == ChannelType.Player)
                return recipient == _session.LocalId;

            return _rooms.Contains(recipient);
        }
        #endregion

        [CanBeNull] private ReceivingState GetOrCreateState(ushort senderId)
        {
            if (senderId >= _receiving.Count || _receiving[senderId] == null || !_receiving[senderId].Peer.IsConnected)
            {
                //Do we know who this ID actually is?
                ClientInfo<TPeer?> info;
                if (!_clients.TryGetClientInfoById(senderId, out info))
                    return null;

                //Fill up list to the necessary index with nulls
                while (_receiving.Count <= senderId)
                    _receiving.Add(null);

                //Create the new state
                _receiving[senderId] = new ReceivingState(info, _events);
            }

            return _receiving[senderId];
        }

        /// <summary>
        /// All the state to do with a remote player we are receiving audio from
        /// </summary>
        private class ReceivingState
        {
            #region fields and properties
            // ReSharper disable once MemberHidesStaticFromOuterClass (Justification: that's totally fine)
            private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(ReceivingState).Name);

            private readonly EventQueue _events;

            public ClientInfo<TPeer?> Peer { get; private set; }

            private DateTime _lastReceiptTime;
            private ushort _baseSequenceNumber;

            private uint _localSequenceNumber;
            public uint SequenceNumber { get { return _localSequenceNumber; } }

            public bool Open { get; private set; }

            public int CurrentChannelSession { get; private set; }
            private readonly Dictionary<int, int> _expectedPerChannelSessions = new Dictionary<int, int>();
            #endregion

            public ReceivingState(ClientInfo<TPeer?> peer, EventQueue events)
            {
                _events = events;

                Peer = peer;
            }

            /// <summary>
            /// Remove all items from _expectedPerChannelSessions which is not in the given list
            /// </summary>
            /// <param name="keys">Keys to keep</param>
            public void ClearChannels([NotNull] List<int> keys)
            {
                //Save how many keys there are in the input. We're going to use this list for two purposes.
                // - [0, Count] is what's in the list now - it's the keys to keep
                // - [Count + 1, N] is what we'll build up in the next loop, that's the keys to *remove*
                var count = keys.Count;

                //Sort the list so it's quicker to search
                keys.Sort();

                //Manually enumerating to prevent foreach loop allocating an enumerator (longstanding Mono/Unity performance problem)
                using (var e = _expectedPerChannelSessions.GetEnumerator())
                {
                    while (e.MoveNext())
                    {
                        //Find the item in the set to keep
                        var item = e.Current;
                        var ti = keys.BinarySearch(0, count, item.Key, Comparer<int>.Default);

                        //We didn't find this item in the list, add it to the end of the list
                        if (ti < 0)
                            keys.Add(item.Key);
                    }
                }

                //We added all the keys we want to *remove* to the end of the list
                //iterate those (starting from the end of the input set)
                for (var i = count; i < keys.Count; i++)
                    _expectedPerChannelSessions.Remove(keys[i]);

                //Remove the items we added to the list so it's in the state we were given
                keys.RemoveRange(count, keys.Count - count);
            }

            /// <summary>
            /// Stop speaking if we've gone for too long without any packets from this peer
            /// </summary>
            /// <param name="now"></param>
            /// <param name="timeout"></param>
            /// <param name="events"></param>
            public void CheckTimeout(DateTime now, TimeSpan timeout, EventQueue events)
            {
                if (!Open)
                    return;

                if ((now - _lastReceiptTime) > timeout)
                {
                    Log.Debug("Client '{0}' timed out active speech session", Peer.PlayerName);
                    StopSpeaking();
                }
            }

            /// <summary>
            /// Check if the channel session for a given compositeId has *changed*
            /// </summary>
            /// <param name="compositeId"></param>
            /// <param name="expectedValue"></param>
            /// <returns></returns>
            public bool CheckChannelSession(int compositeId, int expectedValue)
            {
                var diff = false;
                var none = false;

                //Check if the session ID is either not in the dict, or it's different
                int previousSession;
                if (!_expectedPerChannelSessions.TryGetValue(compositeId, out previousSession))
                    none = true;
                else if (previousSession != expectedValue)
                    diff = true;

                //Overwrite in either case
                if (none || diff)
                    _expectedPerChannelSessions[compositeId] = expectedValue;

                //Return if it was *different*
                return diff;
            }

            public void StopSpeaking()
            {
                if (!Open)
                    throw Log.CreatePossibleBugException("Attempted to stop speaking, but already not speaking", "1A01A7C1-AA5E-41B3-AEA7-4253F0B77237");

                Open = false;

                _events.EnqueueStoppedSpeaking(Peer.PlayerName);
            }

            public bool TryStartSpeaking(ushort startSequenceNumber, int channelSession)
            {
                if (Open)
                    throw Log.CreatePossibleBugException("Attempted to start speaking, but already speaking", "16952260-66DE-4E6F-84E3-F7ED16FC8514");

                // Sequence numbers should always increase (except when they wrap, but that's what the wrapped delta is for). If we receive a smaller sequence number...
                // ...then this must be a voice packet from an old conversation coming in late. Discard it rather than re-opening the session for a single packet.
                if (_baseSequenceNumber.WrappedDelta(startSequenceNumber) < 0)
                    return false;

                // Start speaking, setup up all the speech stream data
                CurrentChannelSession = channelSession;
                _baseSequenceNumber = startSequenceNumber;
                _localSequenceNumber = 0;
                _lastReceiptTime = DateTime.Now;
                Open = true;

                _events.EnqueueStartedSpeaking(Peer.PlayerName);

                return true;
            }

            public bool UpdateSequenceNumber(ushort sequenceNumber)
            {
                // It's possible for a really old packet to come in with a small sequence number than the base sequence number. This can cause chaos because...
                // ...in other places sequence numbers are unsigned but this outdated packet will cause a negative sequence number which will overflow.
                var sequenceDelta = _baseSequenceNumber.WrappedDelta(sequenceNumber);
                if (_localSequenceNumber + sequenceDelta < 0)
                {
                    Log.Trace("Discarding old packet which would cause negative sequence number");
                    return false;
                }

                //Push forward the sequence number
                _localSequenceNumber = (uint)(_localSequenceNumber + sequenceDelta);
                _baseSequenceNumber = sequenceNumber;
                _lastReceiptTime = DateTime.Now;

                return true;
            }
        }
    }
}
