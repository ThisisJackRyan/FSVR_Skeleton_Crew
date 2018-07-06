using System;
using Dissonance.Audio.Codecs;
using Dissonance.Datastructures;
using Dissonance.Extensions;
using Dissonance.Networking;
using NAudio.Wave;

namespace Dissonance.Audio.Playback
{
    internal class DecoderPipeline
        : IDecoderPipeline, IVolumeProvider
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Playback, typeof (DecoderPipeline).Name);

        private readonly Action<DecoderPipeline> _completionHandler;
        private readonly TransferBuffer<EncodedAudio> _inputBuffer;
        private readonly ConcurrentPool<byte[]> _bytePool;
        private readonly BufferedDecoder _source;
        private readonly ISampleSource _output;

        private volatile bool _complete;
        private bool _sourceClosed;

        private TimeSpan _frameDuration;
        private DateTime? _firstFrameArrival;
        private uint _firstFrameSeq;

        public int BufferCount
        {
            get { return _source.BufferCount + _inputBuffer.EstimatedUnreadCount; }
        }
        public float PacketLoss { get { return _source.PacketLoss; } }

        public ChannelPriority Priority { get; private set; }
        public bool Positional { get; private set; }

        private float _remoteAmplitudeMultiplier = 1;

        public WaveFormat OutputFormat { get { return _output.WaveFormat; } }
        #endregion

        #region constructor
        public DecoderPipeline([NotNull] IVoiceDecoder decoder, uint frameSize, [NotNull] Action<DecoderPipeline> completionHandler, bool softClip = true)
        {
            if (decoder == null) throw new ArgumentNullException("decoder");
            if (completionHandler == null) throw new ArgumentNullException("completionHandler");

            _completionHandler = completionHandler;
            _inputBuffer = new TransferBuffer<EncodedAudio>();
            _bytePool = new ConcurrentPool<byte[]>(12, () => new byte[frameSize * decoder.Format.Channels * 4]); // todo wrong frame size (although it should still be large enough)

            _frameDuration = TimeSpan.FromSeconds((double)frameSize / decoder.Format.SampleRate);
            _firstFrameArrival = null;
            _firstFrameSeq = 0;

            var source = new BufferedDecoder(decoder, frameSize, decoder.Format, frame => _bytePool.Put(frame.Data.Array));
            var ramped = new VolumeRampedFrameSource(source, this);
            var samples = new FrameToSampleConverter(ramped);

            ISampleSource toResampler = samples;
            if (softClip)
                toResampler = new SoftClipSampleSource(samples);

            var resampled = new Resampler(toResampler);

            _source = source;
            _output = resampled;
        }
        #endregion

        /// <summary>
        /// Prepare the pipeline to begin playing a new stream of audio
        /// </summary>
        /// <param name="context"></param>
        public void Prepare(SessionContext context)
        {
            _output.Prepare(context);
        }

        public bool Read(ArraySegment<float> samples)
        {
            FlushTransferBuffer();
            var complete = _output.Read(samples);

            if (complete)
                _completionHandler(this);

            return complete;
        }

        /// <summary>
        /// Push a new encoded audio packet
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="now"></param>
        /// <returns>How delayed this packet is from when it should arrive</returns>
        public float Push(VoicePacket packet, DateTime now)
        {
            Log.Trace("Received frame {0} from network", packet.SequenceNumber);

            // copy the data out of the frame, as the network thread will re-use the array
            var array = _bytePool.Get();
            var frame = packet.EncodedAudioFrame.CopyTo(array);

            // queue the frame onto the transfer buffer
            var copy = new EncodedAudio(packet.SequenceNumber, frame);
            if (!_inputBuffer.Write(copy))
                Log.Warn("Failed to write an encoded audio packet into the input transfer buffer");

            //Copy across the stream metadata
            //N.b. doing this means the metadata is surfaced <buffer length> too early
            Priority = packet.Priority;
            Positional = packet.Positional;
            _remoteAmplitudeMultiplier = packet.AmplitudeMultiplier;

            // calculate how late the packet is
            if (!_firstFrameArrival.HasValue)
            {
                _firstFrameArrival = now;
                _firstFrameSeq = packet.SequenceNumber;

                return 0;
            }
            else
            {
                var expectedTime = _firstFrameArrival.Value + TimeSpan.FromTicks(_frameDuration.Ticks * (packet.SequenceNumber - _firstFrameSeq));
                var delay = now - expectedTime;

                return (float)delay.TotalSeconds;
            }
        }

        public void Stop()
        {
            _complete = true;
        }

        public void Reset()
        {
            _output.Reset();

            _firstFrameArrival = null;
            _complete = false;
            _sourceClosed = false;

            VolumeProvider = null;
        }

        private void FlushTransferBuffer()
        {
            // empty the transfer buffer into the decoder buffer
            EncodedAudio frame;
            while (_inputBuffer.Read(out frame))
            {
                _source.Push(frame);
            }

            // set the complete flag after flushing the transfer buffer
            if (_complete && !_sourceClosed)
            {
                _sourceClosed = true;
                _source.Stop();
            }
        }

        #region IVolumeProvider implementation
        public IVolumeProvider VolumeProvider { get; set; }

        float IVolumeProvider.TargetVolume
        {
            get { return (VolumeProvider == null ? 1 : VolumeProvider.TargetVolume) * _remoteAmplitudeMultiplier; }
        }
        #endregion
    }
}
