#if !NCRUNCH

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using Dissonance.Networking;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (DissonanceComms))]
    public class DissonanceCommsEditor : UnityEditor.Editor
    {
        private Texture2D _logo;

        private static GUIStyle _dropdownButtonStyleNormal;
        private static GUIStyle _dropdownButtonStyleToggled;

        private readonly VUMeter _micMeter = new VUMeter("Mic Amplitude");
        private readonly TokenControl _tokenEditor = new TokenControl("These access tokens are used by broadcast/receipt triggers to determine if they should function");

        private bool _micList;

        #region initialisation
        private void CreateSkin()
        {
            if (_dropdownButtonStyleNormal == null)
                _dropdownButtonStyleNormal = new GUIStyle(GUI.skin.button);

            if (_dropdownButtonStyleToggled == null)
            {
                _dropdownButtonStyleToggled = new GUIStyle(GUI.skin.button);
                _dropdownButtonStyleToggled.normal.background = _dropdownButtonStyleToggled.active.background;
            }
        }

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }
        #endregion

        public override void OnInspectorGUI()
        {
            CreateSkin();

            var comm = (DissonanceComms) target;

            GUILayout.Label(_logo);

            CommsNetworkGui();
            DissonanceCommsGui();

            MicrophoneGui(comm);
            PlaybackPrefabGui(comm);

            comm.ChangeWithUndo(
                "Changed Dissonance Mute",
                EditorGUILayout.Toggle("Mute", comm.IsMuted),
                comm.IsMuted,
                a => comm.IsMuted = a
            );

            EditorGUILayout.Space();

            if (Application.isPlaying)
            {
                StatusGui(comm);

                EditorGUILayout.Space();
            }

            _tokenEditor.DrawInspectorGui(comm, comm);

            if (GUILayout.Button("Voice Settings"))
                VoiceSettingsEditor.GoToSettings();

            if (GUILayout.Button("Configure Rooms"))
                ChatRoomSettingsEditor.GoToSettings();
            
            if (GUILayout.Button("Diagnostic Settings"))
                DebugSettingsEditor.GoToSettings();

            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(comm);
        }

        private void MicrophoneGui(DissonanceComms comm)
        {
            string inputString;
            using (new EditorGUILayout.HorizontalScope())
            {
                //Allow the user to type an arbitrary input string
                inputString = EditorGUILayout.DelayedTextField("Microphone Device Name", comm.MicrophoneName ?? "None (Default)");

                //Toggle device list
                if (GUILayout.Button(new GUIContent("Devices"), _micList ? _dropdownButtonStyleToggled : _dropdownButtonStyleNormal, GUILayout.MaxWidth(55)))
                    _micList = !_micList;
            }

            //Show device list
            if (_micList)
            {
                var devices = new List<string> { "None (Default)" };
                devices.AddRange(Microphone.devices);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    foreach (var device in devices)
                    {
                        if (GUILayout.Button(device))
                        {
                            inputString = device;
                            _micList = false;
                        }
                    }
                }
            }

            //If the name is any of these special strings, default it back to null
            var nulls = new[] {
                "null", "(null)",
                "default", "(default)", "none default", "none (default)",
                "none", "(none)"
            };
            if (string.IsNullOrEmpty(inputString) || nulls.Contains(inputString, StringComparer.InvariantCultureIgnoreCase))
                inputString = null;

            if (comm.MicrophoneName != inputString)
            {
                comm.ChangeWithUndo(
                    "Changed Dissonance Microphone",
                    inputString,
                    comm.MicrophoneName,
                    a => comm.MicrophoneName = a
                );
            }

            if (Application.isPlaying)
            {
                var mic = ((IMicrophoneProvider)comm).MicCapture;
                _micMeter.DrawInspectorGui(comm, mic == null ? 0 : mic.Amplitude, comm.IsMuted);
            }
        }

        private static void PlaybackPrefabGui(DissonanceComms comm)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                var prefab = EditorGUILayout.ObjectField("Playback Prefab", comm.PlaybackPrefab, typeof(VoicePlayback), false);
                if (!Application.isPlaying)
                {
                    VoicePlayback newPrefab = null; 
                    if (prefab != null && PrefabUtility.GetPrefabType(prefab) == PrefabType.Prefab)
                        newPrefab = (VoicePlayback)prefab;

                    comm.ChangeWithUndo(
                        "Changed Dissonance Playback Prefab",
                        newPrefab,
                        comm.PlaybackPrefab,
                        a => comm.PlaybackPrefab = a
                    );
                }
            }
        }

        private void CommsNetworkGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<ICommsNetwork>();
            if (nets == null || nets.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "Please attach a Comms Network component appropriate to your networking system to the entity.",
                    MessageType.Error
                );
            }
            else if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the ICommsNetwork components attached to this entity.",
                    MessageType.Error
                );
            }
        }

        private void DissonanceCommsGui()
        {
            var nets = ((DissonanceComms)target).gameObject.GetComponents<DissonanceComms>();
            if (nets.Length > 1)
            {
                EditorGUILayout.HelpBox(
                    "Please remove all but one of the DissonanceComms components attached to this entity.",
                    MessageType.Error
                );
            }
            else
            {
                var comms = FindObjectsOfType<DissonanceComms>();
                if (comms.Length > 1)
                {
                    EditorGUILayout.HelpBox(
                        string.Format("Found {0} DissonanceComms components in scene, please remove all but one", comms.Length),
                        MessageType.Error
                    );
                }
            }
        }

        private static void StatusGui([NotNull] DissonanceComms comm)
        {
            EditorGUILayout.LabelField("Local Player ID", comm.LocalPlayerName);
            EditorGUILayout.LabelField("Estimated Packet Loss", (comm.PacketLoss).ToString("0%"));

            var count = comm.Players.Count - 1;
            EditorGUILayout.LabelField("Peers: (" + (count == 0 ? "none" : count.ToString()) + ")");

            for (var i = 0; i < comm.Players.Count; i++)
            {
                var p = comm.Players[i];

                //Skip the local player
                if (p.Name == comm.LocalPlayerName)
                    continue;

                var message = string.Format("{0} {1} {2} {3}",
                    p.Name,
                    p.IsSpeaking ? "(speaking)" : "",
                    !p.IsConnected ? "(disconnected)" : "",
                    p.Tracker != null && p.Tracker.IsTracking ? "(positional)" : ""
                );

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(message);
                    using (new EditorGUILayout.HorizontalScope())
                        p.IsLocallyMuted = GUILayout.Toggle(p.IsLocallyMuted, new GUIContent("Local Mute"));
                }

                //If there is a player we'll set the comms object to dirty which causes the editor to be redrawn.
                //This makes the (speaking) indicator update live for players.
                EditorUtility.SetDirty(comm);
            }
        }
    }
}

#endif