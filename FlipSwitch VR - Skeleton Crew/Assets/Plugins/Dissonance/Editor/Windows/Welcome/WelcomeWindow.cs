using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    internal class WelcomeWindow
        : BaseDissonanceEditorWindow
    {
        #region constants
        private const float WindowWidth = 300f;
        private const float WindowHeight = 290f;
        private static readonly Vector2 WindowSize = new Vector2(WindowWidth, WindowHeight);

        private const string Title = "Welcome To Dissonance";
        #endregion

        #region construction
        public static void ShowWindow(WelcomeState state)
        {
            var window = GetWindow<WelcomeWindow>(true, Title, true);

            window.minSize = WindowSize;
            window.maxSize = WindowSize;
            window.titleContent = new GUIContent(Title);

            window.State = state;

            window.position = new Rect(150, 150, WindowWidth, WindowHeight);
            window.Repaint();
        }
        #endregion

        #region fields and properties
        public WelcomeState State { get; private set; }
        #endregion

        protected override void DrawContent()
        {
            EditorGUILayout.LabelField("Thankyou for installing Dissonance Voice Chat!", LabelFieldStyle);
            EditorGUILayout.LabelField(string.Format("Version {0}", WelcomeLauncher.CurrentDissonanceVersion), LabelFieldStyle);
            EditorGUILayout.LabelField("", LabelFieldStyle);
            EditorGUILayout.LabelField("Dissonance includes several optional integrations with other assets. Please visit the website to download and install them.", LabelFieldStyle);
            EditorGUILayout.LabelField("", LabelFieldStyle);
            if (GUILayout.Button("Open Integrations List"))
                Application.OpenURL(string.Format("https://placeholder-software.co.uk/dissonance/releases/{0}.html{1}", WelcomeLauncher.CurrentDissonanceVersion, GetQueryString()));
        }

        private static string GetQueryString()
        {
            return EditorMetadata.GetQueryString(
                IntegrationMetadata.FindIntegrations().Select(a => new KeyValuePair<string, string>(a.Id, a.Version.ToString()))
            );
        }
    }
}