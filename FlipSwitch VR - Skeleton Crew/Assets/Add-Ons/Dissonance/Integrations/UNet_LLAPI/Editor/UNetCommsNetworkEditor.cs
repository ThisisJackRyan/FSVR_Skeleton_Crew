using Dissonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Integrations.UNet_LLAPI.Editor
{
    [CustomEditor(typeof(UNetCommsNetwork))]
    public class UNetCommsNetworkEditor
        : BaseDissonnanceCommsNetworkEditor<UNetCommsNetwork, UNetServer, UNetClient, int, ClientConnectionDetails, ServerConnectionDetails>
    {
        private int _maxConnections;
        private SerializedProperty _maxConnectionsProperty;

        private int _port;
        private SerializedProperty _portProperty;

        private SerializedProperty _disableNetworkLifetimeManagementProperty;

        protected void OnEnable()
        {
            _maxConnectionsProperty = serializedObject.FindProperty("_maxConnections");
            _maxConnections = _maxConnectionsProperty.intValue;

            _portProperty = serializedObject.FindProperty("_port");
            _port = _portProperty.intValue;

            _disableNetworkLifetimeManagementProperty = serializedObject.FindProperty("_disableNetworkLifetimeManagement");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                _port = EditorGUILayout.DelayedIntField("Port", _port);
                if (_port >= ushort.MaxValue)
                    EditorGUILayout.HelpBox("Port must be between 0 and 65535", MessageType.Error);
                else
                    _portProperty.intValue = _port;

                _maxConnections = EditorGUILayout.DelayedIntField("Max Connections", _maxConnections);
                if (_maxConnections < 0)
                    EditorGUILayout.HelpBox("Max connections must be > 0", MessageType.Error);
                else
                    _maxConnectionsProperty.intValue = _maxConnections;

                _disableNetworkLifetimeManagementProperty.boolValue = !EditorGUILayout.Toggle("Manage NetworkTransport lifetime", !_disableNetworkLifetimeManagementProperty.boolValue);

                EditorGUILayout.HelpBox(
                    _disableNetworkLifetimeManagementProperty.boolValue
                    ? "Dissonance will not call NetworkTransport.Init or NetworkTransport.Shutdown, you must call them yourself"
                    : "Dissonance will call NetworkTransport.Init and NetworkTransport.Shutdown, this may break your game if you are using UNet for your own networking",
                MessageType.Warning);

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}