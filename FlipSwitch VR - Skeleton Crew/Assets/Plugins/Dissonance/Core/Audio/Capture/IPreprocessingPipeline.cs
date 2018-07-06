using System;
using Dissonance.VAD;
using NAudio.Wave;

namespace Dissonance.Audio.Capture
{
    internal interface IPreprocessingPipeline
        : IDisposable
    {
        /// <summary>
        /// Get the format of audio being output from the pipeline
        /// </summary>
        WaveFormat OutputFormat { get; }

        /// <summary>
        /// Get the amplitude of audio at the end of the pipeline
        /// </summary>
        float Amplitude { get; }

        /// <summary>
        /// Indicates if the pipeline currently requires input audio. For example if VAD is active then this will be true.
        /// </summary>
        bool RequiresInput { get; }

        /// <summary>
        /// Perform any startup work required by the pipeline before audio arrives
        /// </summary>
        void Start();

        /// <summary>
        /// Get the size of input frames
        /// </summary>
        int InputFrameSize { get; }

        /// <summary>
        /// Get the size of input frames
        /// </summary>
        int OutputFrameSize { get; }

        /// <summary>
        /// Get a frame buffer from the preprocessing pipelin
        /// </summary>
        /// <returns></returns>
        float[] GetFrameBuffer();

        /// <summary>
        /// Send a frame of data to the pipeline (array must originally come from `GetFrameBuffer`)
        /// </summary>
        /// <param name="frame"></param>
        void Send(float[] frame);

        /// <summary>
        /// Reset the pipeline back to a clean state
        /// </summary>
        void Reset();

        void Subscribe(IMicrophoneHandler listener);

        bool Unsubscribe(IMicrophoneHandler listener);

        void Subscribe(IVoiceActivationListener listener);

        bool Unsubscribe(IVoiceActivationListener listener);
    }
}
