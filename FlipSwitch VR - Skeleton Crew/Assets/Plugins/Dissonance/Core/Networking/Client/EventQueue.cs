using System;
using System.Collections.Generic;
using Dissonance.Datastructures;
using Dissonance.Threading;

namespace Dissonance.Networking.Client
{
    internal class EventQueue
    {
        #region helper types
        private enum EventType
        {
            PlayerJoined,
            PlayerLeft,
            PlayerStartedSpeaking,
            PlayerStoppedSpeaking,
            VoiceData,
            TextMessage
        }

        private struct NetworkEvent
        {
            public readonly EventType Type;

            public string PlayerName;
            public VoicePacket VoicePacket;
            public TextMessage TextMessage;

            public NetworkEvent(EventType type)
            {
                Type = type;

                PlayerName = null;
                VoicePacket = default(VoicePacket);
                TextMessage = default(TextMessage);
            }
        }
        #endregion

        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(EventQueue).Name);

        private readonly ReadonlyLockedValue<List<NetworkEvent>> _queuedEvents = new ReadonlyLockedValue<List<NetworkEvent>>(new List<NetworkEvent>());

        private readonly IRecycler<byte[]> _pool;

        public event Action<string> PlayerJoined;
        public event Action<string> PlayerLeft;
        public event Action<VoicePacket> VoicePacketReceived;
        public event Action<TextMessage> TextMessageReceived;
        public event Action<string> PlayerStartedSpeaking;
        public event Action<string> PlayerStoppedSpeaking;

        internal event Action<string> OnEnqueuePlayerLeft;
        #endregion

        public EventQueue([NotNull]IRecycler<byte[]> pool)
        {
            if (pool == null)
                throw new ArgumentNullException("pool");

            _pool = pool;
        }

        /// <summary>
        /// Dispatch all events waiting in the queue to event handlers
        /// </summary>
        /// <remarks>Returns true if any invocation caused an error</remarks>
        public bool DispatchEvents()
        {
            var error = false;

            using (var events = _queuedEvents.Lock())
            {
                var queuedEvents = events.Value;

                for (var i = 0; i < queuedEvents.Count; i++)
                {
                    var e = queuedEvents[i];

                    switch (e.Type)
                    {
                        case EventType.PlayerJoined:
                            error |= InvokeEvent(ref e.PlayerName, PlayerJoined);
                            break;
                        case EventType.PlayerLeft:
                            error |= InvokeEvent(ref e.PlayerName, PlayerLeft);
                            break;
                        case EventType.PlayerStartedSpeaking:
                            error |= InvokeEvent(ref e.PlayerName, PlayerStartedSpeaking);
                            break;
                        case EventType.PlayerStoppedSpeaking:
                            error |= InvokeEvent(ref e.PlayerName, PlayerStoppedSpeaking);
                            break;
                        case EventType.VoiceData:
                            error |= InvokeEvent(ref e.VoicePacket, VoicePacketReceived);
                            _pool.Recycle(e.VoicePacket.EncodedAudioFrame.Array);
                            break;
                        case EventType.TextMessage:
                            error |= InvokeEvent(ref e.TextMessage, TextMessageReceived);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                queuedEvents.Clear();

                return error;
            }
        }

        private static bool InvokeEvent<T>(ref T arg, [CanBeNull]Action<T> handler)
        {
            try
            {
                if (handler != null)
                    handler(arg);
            }
            catch (Exception e)
            {
                Log.Error("Exception invoking event handler: {0}", e);
                return true;
            }

            return false;
        }

        #region enqueue
        public void EnqueuePlayerJoined(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerJoined) { PlayerName = playerName });
        }

        public void EnqueuePlayerLeft(string playerName)
        {
            if (OnEnqueuePlayerLeft != null)
                OnEnqueuePlayerLeft(playerName);

            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerLeft) { PlayerName = playerName });
        }

        public void EnqueueStartedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStartedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueStoppedSpeaking(string playerName)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.PlayerStoppedSpeaking) { PlayerName = playerName });
        }

        public void EnqueueVoiceData(VoicePacket data)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.VoiceData) { VoicePacket = data });
        }

        public void EnqueueTextData(TextMessage data)
        {
            using (var events = _queuedEvents.Lock())
                events.Value.Add(new NetworkEvent(EventType.TextMessage) { TextMessage = data });
        }
        #endregion
    }
}
