using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    [InitializeOnLoad]
    public class WelcomeLauncher
    {
        internal static readonly SemanticVersion CurrentDissonanceVersion = new SemanticVersion(3, 0, 2);

        private static readonly string StatePath = Path.Combine(DissonanceRootPath.BaseResourcePath, ".WelcomeState.json");

        /// <summary>
        /// This method will run as soon as the editor is loaded (with Dissonance in the project)
        /// </summary>
        static WelcomeLauncher()
        {
            //Launching the window here caused some issues (presumably it's a bit too early for Unity to handle). Instead we'll wait until the first update call to do it.
            EditorApplication.update += Update;
        }

        // Add a menu item to launch the window
        [MenuItem("Window/Dissonance/Welcome Screen")]
        private static void LaunchInstaller()
        {
            //Clear installer state
            File.Delete(StatePath);

            //Next update will launch the window
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            var state = GetWelcomeState();

            if (!state.ShownForVersion.Equals(CurrentDissonanceVersion.ToString()))
            {
                SetWelcomeState(new WelcomeState(CurrentDissonanceVersion.ToString()));
                WelcomeWindow.ShowWindow(state);
            }

            // We only want to run this once, so unsubscribe from update now that it has run
            // ReSharper disable once DelegateSubtraction (Justification: I know what I'm doing... famous last words)
            EditorApplication.update -= Update;
        }

        [NotNull] private static WelcomeState GetWelcomeState()
        {
            if (!File.Exists(StatePath))
            {
                // State path does not exist at all so create the default
                var state = new WelcomeState("");
                SetWelcomeState(state);
                return state;
            }
            else
            {
                //Read the state from the file
                using (var reader = File.OpenText(StatePath))
                    return JsonUtility.FromJson<WelcomeState>(reader.ReadToEnd());
            }
        }

        private static void SetWelcomeState([CanBeNull]WelcomeState state)
        {
            if (state == null)
            {
                //Clear installer state
                File.Delete(StatePath);
            }
            else
            {
                using (var writer = File.CreateText(StatePath))
                    writer.Write(JsonUtility.ToJson(state));
            }
        }
    }
}