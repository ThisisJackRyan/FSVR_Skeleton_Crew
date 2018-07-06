using System;
using System.Linq;
using Dissonance.Config;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof(VoiceBroadcastTrigger))]
    public class VoiceBroadcastTriggerEditor : UnityEditor.Editor
    {
        private Texture2D _logo;
        private ChatRoomSettings _roomSettings;

        private readonly TokenControl _tokenEditor = new TokenControl("This broadcast trigger will only send voice if the local player has at least one of these access tokens", false);

        private bool _channelTypeExpanded;
        private bool _metadataExpanded;
        private bool _activationModeExpanded;
        private bool _tokensExpanded;
        private bool _ampExpanded;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
            _roomSettings = ChatRoomSettings.Load();
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            var transmitter = (VoiceBroadcastTrigger)target;

            FoldoutBoxGroup(ref _channelTypeExpanded, "Channel Type", ChannelTypeGui, transmitter);
            FoldoutBoxGroup(ref _metadataExpanded, "Channel Metadata", MetadataGUI, transmitter);
            FoldoutBoxGroup(ref _activationModeExpanded, "Activation Mode", ActivationModeGui, transmitter);
            FoldoutBoxGroup(ref _tokensExpanded, "Access Tokens", TokenGui, transmitter);
            FoldoutBoxGroup(ref _ampExpanded, "Amplitude Faders", VolumeGui, transmitter);

            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(target);
        }

        private static void FoldoutBoxGroup(ref bool expanded, string title, Action<VoiceBroadcastTrigger> gui, VoiceBroadcastTrigger trigger)
        {
            expanded = EditorGUILayout.Foldout(expanded, title);
            if (expanded)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    gui(trigger);
        }

        private void ChannelTypeGui(VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Type",
                (CommTriggerTarget)EditorGUILayout.EnumPopup(new GUIContent("Channel Type", "Where this trigger sends voice to"), transmitter.ChannelType),
                transmitter.ChannelType,
                a => transmitter.ChannelType = a
            );

            if (transmitter.ChannelType == CommTriggerTarget.Player)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Channel Transmitter Player Name",
                    EditorGUILayout.TextField(new GUIContent("Recipient Player Name", "The name of the player receiving voice from this trigger"), transmitter.PlayerId),
                    transmitter.PlayerId,
                    a => transmitter.PlayerId = a
                );

                EditorGUILayout.HelpBox("Player mode sends voice data to the specified player.", MessageType.None);
            }

            if (transmitter.ChannelType == CommTriggerTarget.Room)
            {
                var roomNames = _roomSettings.Names;

                var haveRooms = roomNames.Count > 0;
                if (haveRooms)
                {
                    EditorGUILayout.BeginHorizontal();

                    var selectedIndex = string.IsNullOrEmpty(transmitter.RoomName) ? -1 : roomNames.IndexOf(transmitter.RoomName);
                    transmitter.ChangeWithUndo(
                        "Changed Dissonance Transmitter Room",
                        EditorGUILayout.Popup(new GUIContent("Chat Room", "The room to send voice to"), selectedIndex, roomNames.Select(a => new GUIContent(a)).ToArray()),
                        selectedIndex,
                        a => transmitter.RoomName = roomNames[a]
                    );

                    if (GUILayout.Button("Config Rooms"))
                        ChatRoomSettingsEditor.GoToSettings();

                    EditorGUILayout.EndHorizontal();

                    if (string.IsNullOrEmpty(transmitter.RoomName))
                        EditorGUILayout.HelpBox("Please select a chat room", MessageType.Warning);
                    else if (!roomNames.Contains(transmitter.RoomName))
                        EditorGUILayout.HelpBox(string.Format("Room '{0}' is no longer defined in the chat room configuration! \nRe-create the '{0}' room, or select a different room.", transmitter.RoomName), MessageType.Warning);
                }
                else
                {
                    if (GUILayout.Button("Create New Rooms"))
                        ChatRoomSettingsEditor.GoToSettings();
                }

                EditorGUILayout.HelpBox("Room mode sends voice data to all players in the specified room.", MessageType.None);

                if (!haveRooms)
                    EditorGUILayout.HelpBox("No rooms are defined. Click 'Create New Rooms' to configure chat rooms.", MessageType.Warning);
            }

            if (transmitter.ChannelType == CommTriggerTarget.Self)
            {
                EditorGUILayout.HelpBox(
                    "Self mode sends voice data to the DissonancePlayer attached to this game object.",
                    MessageType.None
                );

                var player = transmitter.GetComponent<IDissonancePlayer>();
                if (player == null)
                {
                    EditorGUILayout.HelpBox(
                        "This entity has no Dissonance player component!",
                        MessageType.Error
                    );
                }
                else if (Application.isPlaying && player.Type == NetworkPlayerType.Local)
                {
                    EditorGUILayout.HelpBox(
                        "This is the local player.\n" +
                        "Are you sure you mean to broadcast to the local player?",
                        MessageType.Warning
                    );
                }
            }
        }

        private static void MetadataGUI(VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Positional Audio",
                EditorGUILayout.Toggle(new GUIContent("Use Positional Data", "If voices sent with this trigger should be played with 3D playback"), transmitter.BroadcastPosition),
                transmitter.BroadcastPosition,
                a => transmitter.BroadcastPosition = a
            );

            if (!transmitter.BroadcastPosition)
            {
                EditorGUILayout.HelpBox(
                    "Send audio on this channel with positional data to allow 3D playback if set up on the receiving end. There is no performance cost to enabling this.\n\n" +
                    "Please see the Dissonance documentation for instructions on how to set your project up for playback of 3D voice comms.",
                    MessageType.Info);
            }

            transmitter.ChangeWithUndo(
                "Changed Dissonance Channel Priority",
                (ChannelPriority)EditorGUILayout.EnumPopup(new GUIContent("Priority", "Priority for speech sent through this trigger"), transmitter.Priority),
                transmitter.Priority,
                a => transmitter.Priority = a
            );

            if (transmitter.Priority == ChannelPriority.None)
            {
                EditorGUILayout.HelpBox(
                    "Priority for the voice sent from this room. Voices will mute all lower priority voices on the receiver while they are speaking.\n\n" +
                    "'None' means that this room specifies no particular priority and the priority of this player will be used instead",
                    MessageType.Info);
            }
        }

        private static void ActivationModeGui(VoiceBroadcastTrigger transmitter)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Activation Mode",
                (CommActivationMode)EditorGUILayout.EnumPopup(new GUIContent("Activation Mode", "How the user should indicate an intention to speak"), transmitter.Mode),
                transmitter.Mode,
                a => transmitter.Mode = a
            );

            if (transmitter.Mode == CommActivationMode.None)
            {
                EditorGUILayout.HelpBox(
                    "While in this mode no voice will ever be transmitted",
                    MessageType.Info
                );
            }

            if (transmitter.Mode == CommActivationMode.PushToTalk)
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Push To Talk Axis",
                    EditorGUILayout.TextField(new GUIContent("Input Axis Name", "Which input axis indicates the user is speaking"), transmitter.InputName),
                    transmitter.InputName,
                    a => transmitter.InputName = a
                );

                EditorGUILayout.HelpBox(
                    "Define an input axis in Unity's input manager if you have not already.",
                    MessageType.Info
                );
            }

            VolumeTriggerActivationGui(transmitter);
        }

        private static void VolumeTriggerActivationGui(VoiceBroadcastTrigger transmitter)
        {
            using (var toggle = new EditorGUILayout.ToggleGroupScope(new GUIContent("Collider Volume Activation", "Only allows speech when the user is inside a collider"), transmitter.UseTrigger))
            {
                transmitter.ChangeWithUndo(
                    "Changed Dissonance Trigger Activation",
                    toggle.enabled,
                    transmitter.UseTrigger,
                    a => transmitter.UseTrigger = a
                );

                if (transmitter.UseTrigger)
                {
                    if (!transmitter.gameObject.GetComponents<Collider>().Any(c => c.isTrigger))
                        EditorGUILayout.HelpBox("Cannot find any collider triggers attached to this entity.", MessageType.Warning);
                    //if (!transmitter.gameObject.GetComponents<Rigidbody>().Any() && !transmitter.gameObject.GetComponents<CharacterController>().Any())
                    //    EditorGUILayout.HelpBox("Cannot find either a RigidBody nor CharacterController attached to this entity (required for triggers to work).", MessageType.Warning);
                }
            }

            if (!transmitter.UseTrigger)
            {
                EditorGUILayout.HelpBox(
                    "Use trigger activation to only broadcast when the player is inside a trigger volume.",
                    MessageType.Info
                );
            }
        }

        private void TokenGui(VoiceBroadcastTrigger transmitter)
        {
            _tokenEditor.DrawInspectorGui(transmitter, transmitter);
        }

        private static void VolumeGui(VoiceBroadcastTrigger transmitter)
        {
            EditorGUILayout.LabelField(new GUIContent(string.Format("{0} Fade", transmitter.Mode), string.Format("Fade when {0} mode changes", transmitter.Mode)));
            SingleFaderGui(transmitter, transmitter.ActivationFader);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(!transmitter.UseTrigger))
            {
                EditorGUILayout.LabelField(new GUIContent("Volume Trigger Fade", "Fade when when entering/exiting collider volume trigger"));
                SingleFaderGui(transmitter, transmitter.ColliderTriggerFader);
            }
        }

        private static void SingleFaderGui(VoiceBroadcastTrigger transmitter, VolumeFaderSettings settings)
        {
            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Volume",
                EditorGUILayout.Slider(new GUIContent("Channel Volume", "Volume multiplier for voice sent from this trigger"), settings.Volume, 0, 2),
                settings.Volume,
                a => settings.Volume = a
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade In Time",
                EditorGUILayout.Slider(new GUIContent("Fade In Time", "Duration (seconds) for voice take to reach full volume"), (float)settings.FadeIn.TotalSeconds, 0, 3),
                settings.FadeIn.TotalSeconds,
                a => settings.FadeIn = TimeSpan.FromSeconds(a)
            );

            transmitter.ChangeWithUndo(
                "Changed Dissonance Trigger Fade Out Time",
                EditorGUILayout.Slider(new GUIContent("Fade Out Time", "Duration (seconds) for voice to fade to silent and stop transmitting"), (float)settings.FadeOut.TotalSeconds, 0, 3),
                settings.FadeOut.TotalSeconds,
                a => settings.FadeOut = TimeSpan.FromSeconds(a)
            );
        }
    }
}
