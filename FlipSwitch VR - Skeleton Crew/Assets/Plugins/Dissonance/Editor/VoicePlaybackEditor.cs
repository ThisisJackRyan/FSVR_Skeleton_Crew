﻿using Dissonance.Audio.Playback;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (VoicePlayback))]
    [CanEditMultipleObjects]
    public class VoicePlaybackEditor : UnityEditor.Editor
    {
        private Texture2D _logo;

        private readonly VUMeter _amplitudeMeter = new VUMeter("Amplitude");

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            if (!Application.isPlaying)
                return;

            var player = (VoicePlayback)target;

            EditorGUILayout.LabelField("Player Name", player.PlayerName);
            EditorGUILayout.LabelField("Positional Playback Available", player.PositionTrackingAvailable.ToString());
            EditorGUILayout.LabelField("Priority", player.Priority.ToString());
            EditorGUILayout.LabelField("Packet Loss", string.Format("{0}%", player.PacketLoss ?? 0));
            EditorGUILayout.LabelField("Network Jitter", string.Format("{0}σms", player.Jitter * 1000));

            _amplitudeMeter.DrawInspectorGui(target, player.Amplitude, !player.IsSpeaking);

            if (player.ApplyingAudioSpatialization)
            {
                EditorGUILayout.LabelField("Playback Mode", "Internally Spatialized");
                EditorGUILayout.HelpBox("Dissonance has detected that the AudioSource is not spatialized by an external audio spatializer. Dissonance will apply basic spatialization.", MessageType.Info, true);
            }
            else
            {
                EditorGUILayout.LabelField("Playback Mode", "Externally Spatialized");
                EditorGUILayout.HelpBox("Dissonance has detected that the AudioSource is spatialized by an external audio spatializer.", MessageType.Info, true);
            }

            EditorUtility.SetDirty(player);
        }
    }
}
