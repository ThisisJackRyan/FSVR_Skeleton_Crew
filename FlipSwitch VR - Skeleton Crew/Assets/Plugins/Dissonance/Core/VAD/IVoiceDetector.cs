using System;

namespace Dissonance.VAD
{
    internal interface IVoiceDetector
        : IDisposable
    {
        /// <summary>
        /// Get a value indicating if the VAD is currently detecting speech
        /// </summary>
        bool IsSpeaking { get; }

        /// <summary>
        /// Reset the VAD to handle a new audio stream
        /// </summary>
        void Reset();

        /// <summary>
        /// Handle a frame of audio
        /// </summary>
        /// <param name="buffer"></param>
        void Handle(ArraySegment<short> buffer);
    }
}