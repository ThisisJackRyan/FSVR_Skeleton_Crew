﻿using System.Linq;
using Dissonance.Config;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    [CustomEditor(typeof (VoiceReceiptTrigger))]
    public class VoiceReceiptTriggerEditor : UnityEditor.Editor
    {
        private Texture2D _logo;
        private ChatRoomSettings _roomSettings;

        private readonly TokenControl _tokenEditor = new TokenControl("This receipt trigger will only receive voice if the local player has at least one of these access tokens");

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
            _roomSettings = ChatRoomSettings.Load();
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            var receiver = (VoiceReceiptTrigger) target;

            RoomsGui(receiver);
            EditorGUILayout.Space();

            _tokenEditor.DrawInspectorGui(receiver, receiver);
            EditorGUILayout.Space();

            TriggerActivationGui(receiver);

            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(target);
        }

        private void RoomsGui(VoiceReceiptTrigger trigger)
        {
            var roomNames = _roomSettings.Names;

            var haveRooms = roomNames.Count > 0;
            if (haveRooms)
            {
                EditorGUILayout.BeginHorizontal();

                var selectedIndex = string.IsNullOrEmpty(trigger.RoomName) ? -1 : roomNames.IndexOf(trigger.RoomName);
                trigger.ChangeWithUndo(
                    "Changed Dissonance Receiver Room",
                    EditorGUILayout.Popup("Chat Room", selectedIndex, roomNames.ToArray()),
                    selectedIndex,
                    a => trigger.RoomName = roomNames[a]
                );

                if (GUILayout.Button("Config Rooms"))
                    ChatRoomSettingsEditor.GoToSettings();

                EditorGUILayout.EndHorizontal();

                if (string.IsNullOrEmpty(trigger.RoomName))
                    EditorGUILayout.HelpBox("Please select a chat room", MessageType.Warning);
                else if (!roomNames.Contains(trigger.RoomName))
                    EditorGUILayout.HelpBox(string.Format("Room '{0}' is no longer defined in the chat room configuration! \nRe-create the '{0}' room, or select a different room.", trigger.RoomName), MessageType.Warning);
            }
            else
            {
                if (GUILayout.Button("Create New Rooms"))
                    ChatRoomSettingsEditor.GoToSettings();
            }

            if (!haveRooms)
                EditorGUILayout.HelpBox("No rooms are defined. Click 'Create New Rooms' to configure chat rooms.", MessageType.Warning);
        }

        private static void TriggerActivationGui(VoiceReceiptTrigger trigger)
        {
            using (var scope = new EditorGUILayout.ToggleGroupScope("Trigger Activation", trigger.UseTrigger))
            {
                trigger.ChangeWithUndo(
                    "Changed Dissonance Trigger Activation",
                    scope.enabled,
                    trigger.UseTrigger,
                    a => trigger.UseTrigger = a
                );

                EditorGUILayout.HelpBox(
                    "Use trigger activation to only receive when the player is inside a trigger volume.",
                    MessageType.Info);

                if (trigger.UseTrigger)
                {
                    if (!trigger.gameObject.GetComponents<Collider>().Any(c => c.isTrigger))
                        EditorGUILayout.HelpBox("Cannot find any collider triggers attached to this entity.", MessageType.Warning);
                    if (!trigger.gameObject.GetComponents<Rigidbody>().Any() && !trigger.gameObject.GetComponents<CharacterController>().Any())
                        EditorGUILayout.HelpBox("Cannot find either a RigidBody nor CharacterController attached to this entity (required for triggers to work).", MessageType.Warning);
                }
            }
        }
    }
}
