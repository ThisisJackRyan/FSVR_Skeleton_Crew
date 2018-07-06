namespace Dissonance.Audio.Capture
{
    internal interface IMicrophoneProvider
    {
        MicrophoneCapture MicCapture { get; }
    }
}
