using System;
using Dissonance.Datastructures;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    /// <summary>
    /// A sample provider which reads from an internal buffer of samples
    /// </summary>
    internal class BufferedSampleProvider
        : ISampleProvider
    {
        public int Count
        {
            get { return _samples.EstimatedUnreadCount; }
        }

        private readonly WaveFormat _format;
        /// <summary>
        /// Format of the samples in this provider
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return _format; }
        }

        private readonly TransferBuffer<float> _samples;

        public BufferedSampleProvider(WaveFormat format, int bufferSize)
        {
            _format = format;
            _samples = new TransferBuffer<float>(bufferSize);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (!_samples.Read(new ArraySegment<float>(buffer, offset, count)))
                return 0;
            return count;
        }

        public int Write(ArraySegment<float> data)
        {
            return Write(data.Array, data.Offset, data.Count);
        }

        public int Write(float[] buffer, int offset, int count)
        {
            //Write as much as possible. That's either the amount passed in, or however much space is left in the buffer
            var writeCount = Math.Min(_samples.Capacity - _samples.EstimatedUnreadCount, count);

            if (_samples.Write(new ArraySegment<float>(buffer, offset, writeCount)))
                return writeCount;

            return 0;
        }

        public void Reset()
        {
            _samples.Clear();
        }
    }
}
