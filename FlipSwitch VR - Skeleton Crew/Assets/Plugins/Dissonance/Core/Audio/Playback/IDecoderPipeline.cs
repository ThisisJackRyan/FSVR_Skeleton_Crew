using System;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal interface IDecoderPipeline
    {
        int BufferCount { get; }
        float PacketLoss { get; }

        ChannelPriority Priority { get; }
        bool Positional { get; }

        [NotNull] WaveFormat OutputFormat { get; }

        void Prepare(SessionContext context);
        bool Read(ArraySegment<float> samples);
    }
}