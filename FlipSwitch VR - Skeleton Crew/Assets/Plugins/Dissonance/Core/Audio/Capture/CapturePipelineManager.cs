using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dissonance.Networking;
using Dissonance.VAD;

namespace Dissonance.Audio.Capture
{
    internal class CapturePipelineManager
        : IMicrophoneProvider
    {
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(CapturePipelineManager).Name);

        private readonly CodecSettings _codecSettings;
        private readonly RoomChannels _roomChannels;
        private readonly PlayerChannels _playerChannels;
        private readonly PacketLossMonitor _packetLoss;
        [CanBeNull] private ICommsNetwork _network;

        [CanBeNull] public MicrophoneCapture MicCapture { get; private set; }
        private EncoderPipeline _transmissionPipeline;

        private readonly List<IVoiceActivationListener> _activationListeners = new List<IVoiceActivationListener>();

        private string _micName;
        public string MicrophoneName
        {
            get { return _micName; }
            set
            {
                if (_micName == value)
                    return;

                _micName = value;
                RestartTransmissionPipeline();
            }
        }

        public float PacketLoss { get { return _packetLoss.PacketLoss; } }

        public CapturePipelineManager([NotNull] CodecSettings codecSettings, [NotNull] RoomChannels roomChannels, [NotNull] PlayerChannels playerChannels, [NotNull] ReadOnlyCollection<VoicePlayerState> players)
        {
            if (codecSettings == null) throw new ArgumentNullException("codecSettings");
            if (roomChannels == null) throw new ArgumentNullException("roomChannels");
            if (playerChannels == null) throw new ArgumentNullException("playerChannels");
            if (players == null) throw new ArgumentNullException("players");

            _codecSettings = codecSettings;
            _roomChannels = roomChannels;
            _playerChannels = playerChannels;
            _packetLoss = new PacketLossMonitor(players);
        }

        public void Start([NotNull] ICommsNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException("network");

            _network = network;

            Net_ModeChanged(network.Mode);
            network.ModeChanged += Net_ModeChanged;
        }

        public void Destroy()
        {
            if (!ReferenceEquals(_network, null))
                _network.ModeChanged -= Net_ModeChanged;

            StopTransmissionPipeline();
        }

        private void Net_ModeChanged(NetworkMode mode)
        {
            if (mode.IsClientEnabled())
                RestartTransmissionPipeline();
            else
                StopTransmissionPipeline();
        }

        public void Update(bool muted, float deltaTime)
        {
			_packetLoss.Update();

            if (MicCapture != null && MicCapture.Update() || DetectFrameSkip(deltaTime))
                RestartTransmissionPipeline();

            if (_transmissionPipeline != null)
            {
                _transmissionPipeline.Update(muted);
                _transmissionPipeline.Encoder.PacketLoss = _packetLoss.PacketLoss;
            }
        }

        private bool DetectFrameSkip(float deltaTime)
        {
            // If frame rate is less than 8 frames per second reset the transmission pipeline. If FPS is actually 8 then this will break
            // voice transmission (due to reset every frame) but that's fine because we can't really run good voice at 8fps anyway. In the
            // more likely case that this was just a stutter in frame rate it will reset the pipeline and fix the mic desync.
            var skip = deltaTime > 0.125f;
            if (skip)
                Log.Warn("Detected a frame skip of {0}ms, forcing reset of Microphone capture pipeline", deltaTime * 1000f);

            return skip;
        }

        private void StopTransmissionPipeline()
        {
            //Tear down old capture system
            if (MicCapture != null)
            {
                for (var i = 0; i < _activationListeners.Count; i++)
                    MicCapture.Unsubscribe(_activationListeners[i]);

                MicCapture.Dispose();
                MicCapture = null;
            }

            //Tear down old encoding system
            if (_transmissionPipeline != null)
            {
                _transmissionPipeline.Dispose();
                _transmissionPipeline = null;
            }
        }

        private void RestartTransmissionPipeline()
        {
            StopTransmissionPipeline();

            //No point starting a transmission pipeline if the network is not a client
            if (_network == null || !_network.Mode.IsClientEnabled())
                return;

            //Create new mic capture system
            MicCapture = MicrophoneCapture.Start(_micName);

            //If we created a mic (can be null if e.g. there is no mic)
            if (MicCapture != null)
            {
                //Refresh all channels
                _roomChannels.Refresh();
                _playerChannels.Refresh();

                //Sub VAD listeners
                for (var i = 0; i < _activationListeners.Count; i++)
                    MicCapture.Subscribe(_activationListeners[i]);

                //Create encoder
                var encoder = _codecSettings.CreateEncoder();
                _transmissionPipeline = new EncoderPipeline(MicCapture, encoder, _network, () => _playerChannels.Count + _roomChannels.Count);
            }
            else
                Log.Warn("No microphone detected; local voice transmission will be disabled.");
        }

        #region VAD subscribers
        public void Subscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot subscribe with a null listener");

            _activationListeners.Add(listener);

            if (MicCapture != null)
                MicCapture.Subscribe(listener);
        }

        public void Unsubscribe([NotNull] IVoiceActivationListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException("listener", "Cannot unsubscribe with a null listener");

            _activationListeners.Remove(listener);

            if (MicCapture != null)
                MicCapture.Unsubscribe(listener);
        }
        #endregion

#if UNITY_EDITOR

        public void Pause()
        {
            StopTransmissionPipeline();
        }

        public void Resume()
        {
            RestartTransmissionPipeline();
        }
#endif
    }
}
