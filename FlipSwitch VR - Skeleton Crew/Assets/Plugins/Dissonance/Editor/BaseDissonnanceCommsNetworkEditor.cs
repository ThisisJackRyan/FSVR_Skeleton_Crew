using System;
using Dissonance.Networking;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor
{
    public class BaseDissonnanceCommsNetworkEditor<T, TS, TC, TP, CP, SP>
        : UnityEditor.Editor
        where T : BaseCommsNetwork<TS, TC, TP, CP, SP>
        where TS : BaseServer<TS, TC, TP>
        where TC : BaseClient<TS, TC, TP>
        where TP : struct, IEquatable<TP>
    {
        private Texture2D _logo;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label(_logo);

            var network = (T)target;

            if (Application.isPlaying)
            {
                GUILayout.Label("Network Stats");

                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;
                try
                {
                    network.OnInspectorGui();
                    EditorUtility.SetDirty(network);
                }
                finally
                {
                    EditorGUI.indentLevel = indent;
                }
            }
        }
    }
}
