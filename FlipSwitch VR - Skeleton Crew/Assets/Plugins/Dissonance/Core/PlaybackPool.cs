using System;
using Dissonance.Audio.Playback;
using Dissonance.Datastructures;
using NAudio.Wave;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dissonance
{
    internal class PlaybackPool
    {
        private readonly Pool<VoicePlayback> _pool;

        private readonly CodecSettings _codecSettings;
        private readonly IPriorityManager _priority;

        private VoicePlayback _prefab;
        private Transform _parent;

        public PlaybackPool(CodecSettings codecSettings, IPriorityManager priority)
        {
            _codecSettings = codecSettings;
            _priority = priority;
            _pool = new Pool<VoicePlayback>(6, CreatePlayback);
        }

        public void Start(VoicePlayback playbackPrefab, Transform transform)
        {
            _prefab = playbackPrefab;
            _parent = transform;
        }

        private VoicePlayback CreatePlayback()
        {
            //The game object must be inactive when it's added to the scene (so it can be edited before it activates)
            _prefab.gameObject.SetActive(false);

            //Create an instance (currently inactive)
            var entity = Object.Instantiate(_prefab.gameObject);
            entity.transform.parent = _parent;

            //Configure (and add, if necessary) audio source
            var audioSource = entity.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = entity.AddComponent<AudioSource>();
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.bypassReverbZones = true;
            }
            audioSource.loop = true;
            audioSource.pitch = 1;
            audioSource.clip = null;
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            audioSource.spatialBlend = 1;
            audioSource.Stop();

            //Configure (and add, if necessary) sample player
            //Because the audio source has no clip, this filter will be "played" instead
            var player = entity.GetComponent<SamplePlaybackComponent>();
            if (player == null)
                entity.AddComponent<SamplePlaybackComponent>();

            //Configure VoicePlayback component
            var playback = entity.GetComponent<VoicePlayback>();
            playback.SetFormat(new WaveFormat(1, _codecSettings.SampleRate), _codecSettings.FrameSize);
            playback.PriorityManager = _priority;

            return playback;
        }

        public VoicePlayback Get(string playerId)
        {
            if (playerId == null)
                throw new ArgumentNullException("playerId");

            var instance = _pool.Get();

            instance.gameObject.name = string.Format("Player {0} voice comms", playerId);
            instance.PlayerName = playerId;

            return instance;
        }

        public void Put(VoicePlayback playback)
        {
            if (playback == null)
                throw new ArgumentNullException("playback");

            playback.gameObject.SetActive(false);
            playback.gameObject.name = "Spare voice comms";
            playback.PlayerName = null;

            _pool.Put(playback);
        }
    }
}
