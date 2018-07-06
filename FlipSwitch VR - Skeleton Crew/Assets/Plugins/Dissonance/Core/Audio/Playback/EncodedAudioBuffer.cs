using System;
using System.Threading;
using Dissonance.Datastructures;
using HandyCollections.Heap;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Buffers encoded audio packets as they arrive, and delivers them in order when requested.
    /// </summary>
    internal class EncodedAudioBuffer
    {
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (EncodedAudioBuffer).Name);
        
        private readonly MinHeap<EncodedAudio> _heap;
        private readonly Action<EncodedAudio> _droppedFrameHandler;

        private volatile bool _complete;
        private int _count;

        public int Count
        {
            get { return _count; }
        }

        public uint SequenceNumber { get; private set; }

        private readonly PacketLossCalculator _loss = new PacketLossCalculator(128);
        public float PacketLoss { get { return _loss.PacketLoss; } }

        public EncodedAudioBuffer([NotNull] Action<EncodedAudio> droppedFrameHandler)
        {
            if (droppedFrameHandler == null) throw new ArgumentNullException("droppedFrameHandler");

            _droppedFrameHandler = droppedFrameHandler;
            _heap = new MinHeap<EncodedAudio>(new EncodedAudio.Comparer()) { AllowHeapResize = true };
            SequenceNumber = 0;
            _complete = false;
        }

        public void Push(EncodedAudio frame)
        {
            Log.Trace("Buffering encoded audio frame {0}", frame.Sequence);

            _heap.Add(frame);
            Interlocked.Increment(ref _count);

            if (_heap.Count > 40)
                Log.Warn(Log.PossibleBugMessage(string.Format("Encoded audio heap is getting very large ({0} items)", _heap.Count), "59EE0102-FF75-467A-A50D-00BF670E9B8C"));
        }

        public void Stop()
        {
            Log.Trace("Stopping");
            _complete = true;
        }

        /// <summary>
        ///     Reads the next frame from the buffer.
        /// </summary>
        /// <param name="frame">The next frame to play. May return <c>null</c> if the frame has not been received.</param>
        /// <returns><c>true</c> if there are more frames available; else <c>false</c></returns>
        public bool Read(out EncodedAudio? frame)
        {
            var expected = SequenceNumber;

            // remove frames which we have already skipped past
            while (_heap.Count > 0 && _heap.Minimum.Sequence < expected)
            {
                var dropped = _heap.RemoveMin();
                Interlocked.Decrement(ref _count);

                var difference = expected - dropped.Sequence;
                Log.Trace("Discarding late encoded audio frame from buffer ({0} packets late)", difference);

                if (difference > 30)
                    Log.Warn(Log.PossibleBugMessage(string.Format("Received a very late packet ({0} packets late)", difference), "30EF1B03-7BBC-49D3-A23E-6E84781FF29F"));

                _droppedFrameHandler(dropped);
            }

            if (_heap.Count > 0 && _heap.Minimum.Sequence == expected)
            {
                // the next frame is the one we are looking for
                frame = _heap.RemoveMin();
                Interlocked.Decrement(ref _count);
                Log.Trace("Retrieved frame {0} from buffer ({1} items remain in buffer)", frame.Value.Sequence, _heap.Count);

                _loss.Update(true);
            }
            else
            {
                // we don't have the next frame yet
                frame = null;
                Log.Trace("Dropped frame {0}; audio frame not available when requested", expected);

                _loss.Update(false);
            }

            SequenceNumber++;
            return IsComplete();
        }

        public void Reset()
        {
            Log.Trace("Resetting");

            while (_heap.Count > 0)
            {
                _droppedFrameHandler(_heap.RemoveMin());
                Interlocked.Decrement(ref _count);
            }

            _loss.Clear();
            _complete = false;
            SequenceNumber = 0;
        }

        private bool IsComplete()
        {
            return _complete && _heap.Count == 0;
        }
    }
}