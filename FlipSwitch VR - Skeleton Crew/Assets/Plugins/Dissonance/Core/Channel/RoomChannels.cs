﻿using Dissonance.Audio.Capture;

namespace Dissonance
{
    /// <summary>
    /// A collection of channels to rooms
    /// </summary>
    public sealed class RoomChannels
        : Channels<RoomChannel, string>
    {
        internal RoomChannels(IChannelPriorityProvider priorityProvider)
            : base(priorityProvider)
        {
            OpenedChannel += (id, _) => Log.Debug("Opened channel to room '{0}'", id);
            ClosedChannel += (id, _) => Log.Debug("Closed channel to room '{0}'", id);
        }

        protected override RoomChannel CreateChannel(ushort subscriptionId, string channelId, ChannelProperties properties)
        {
            return new RoomChannel(subscriptionId, channelId, this, properties);
        }
    }
}
