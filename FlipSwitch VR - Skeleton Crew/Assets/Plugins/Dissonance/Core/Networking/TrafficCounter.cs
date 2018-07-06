using System;
using System.Collections.Generic;

namespace Dissonance.Networking
{
    internal class TrafficCounter
    {
        public int Packets { get; private set; }
        public int Bytes { get; private set; }
        public int BytesPerSecond { get; private set; }

        private int _runningTotal;
        private readonly Queue<KeyValuePair<DateTime, int>> _updated = new Queue<KeyValuePair<DateTime, int>>(64); 

        public void Update(int bytes, DateTime? now = null)
        {
            Packets++;
            Bytes += bytes;

            //Store the update in a queue, keyed by time
            var time = now ?? DateTime.Now;
            _updated.Enqueue(new KeyValuePair<DateTime, int>(time, bytes));
            _runningTotal += bytes;

            //Remove the oldest value if it's over 10 seconds old
            if (time - _updated.Peek().Key > TimeSpan.FromSeconds(10))
            {
                var removed = _updated.Dequeue();
                _runningTotal -= removed.Value;

                //Calculate bytes per second, now that we have a 10 second window
                BytesPerSecond = _runningTotal / 10;
            }
        }

        public override string ToString()
        {
            return Format(Packets, Bytes, BytesPerSecond);
        }

        public static void Combine(out int packets, out int bytes, out int totalBytesPerSecond, params TrafficCounter[] counters)
        {
            packets = 0;
            bytes = 0;
            totalBytesPerSecond = 0;

            foreach (var counter in counters)
            {
                if (counter == null)
                    continue;

                packets += counter.Packets;
                bytes += counter.Bytes;
                totalBytesPerSecond += counter.BytesPerSecond;
            }
        }

        public static string Format(int packets, int bytes, int bytesPerSecond)
        {
            return string.Format("{0} in {1:N0}pkts at {2}/s", FormatByteString(bytes), packets, FormatByteString(bytesPerSecond));
        }

        private static string FormatByteString(float bytes)
        {
            const float kb = 1024;
            const float mb = kb * 1024;

            string suffix;

            if (bytes >= mb)
            {
                bytes /= mb;
                suffix = "MiB";
            }
            else if (bytes >= kb)
            {
                bytes /= kb;
                suffix = "KiB";
            }
            else
                suffix = "B";

            return string.Format("{0:0.0}{1}", bytes, suffix);
        }
    }
}
