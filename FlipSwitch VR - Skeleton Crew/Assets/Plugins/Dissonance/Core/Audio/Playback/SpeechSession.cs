using System;
using NAudio.Wave;
using UnityEngine;

namespace Dissonance.Audio.Playback
{
    /// <summary>
    ///     Represents a decoder pipeline for a single playback session.
    /// </summary>
    public struct SpeechSession
    {
        #region fields and properties
        private const float MinimumDelay = 0.050f;
        private const float MaximumDelay = 0.750f;
        private static readonly float InitialBufferDelay = 0.1f;

        private readonly IDecoderPipeline _pipeline;
        private readonly SessionContext _context;
        private readonly DateTime _creationTime;
        private readonly IJitterEstimator _jitter;

        public int BufferCount { get { return _pipeline.BufferCount; } }
        public SessionContext Context { get { return _context; } }
        public ChannelPriority Priority { get { return _pipeline.Priority; } }
        public bool Positional { get { return _pipeline.Positional; } }
        [NotNull] public WaveFormat OutputWaveFormat { get { return _pipeline.OutputFormat; } }
        internal float PacketLoss { get { return _pipeline.PacketLoss; } }

        public DateTime TargetActivationTime
        {
            get { return _creationTime + Delay; }
        }

        private DateTime _startTime;
        public DateTime ActivationTime
        {
            get { return _startTime + Delay; }
        }

        public TimeSpan Delay
        {
            get
            {
                //Calculate how much we should be delayed based purely on the jitter measurement
                //
                // Jitter value is the standard deviation of jitter, so we'll keep a buffer of 2.5 standard deviations. This means about
                // 97.5 % of packets will arrive within the buffer time. Assuming normally distributed jitter, which isn't true but it's
                // at least a good guesstimate for now.
                var jitterDelay = Mathf.LerpUnclamped(InitialBufferDelay, _jitter.Jitter * 2.5f, _jitter.Confidence);

                return TimeSpan.FromSeconds(Mathf.Clamp(jitterDelay, MinimumDelay, MaximumDelay));
            }
        }
        #endregion

        private SpeechSession(SessionContext context, IJitterEstimator jitter, IDecoderPipeline pipeline, DateTime now)
        {
            _context = context;
            _pipeline = pipeline;
            _creationTime = now;
            _jitter = jitter;

            _startTime = now;
        }

        internal static SpeechSession Create(SessionContext context, IJitterEstimator jitter, IDecoderPipeline pipeline, DateTime now)
        {
            return new SpeechSession(context, jitter, pipeline, now);
        }

        public void Prepare(DateTime now)
        {
            _startTime = now - Delay;
            _pipeline.Prepare(_context);
        }

        /// <summary>
        ///     Pulls the specfied number of samples from the pipeline, decoding packets as necessary.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns><c>true</c> if there are more samples available; else <c>false</c>.</returns>
        public bool Read(ArraySegment<float> samples)
        {
            return _pipeline.Read(samples);
        }
    }
}