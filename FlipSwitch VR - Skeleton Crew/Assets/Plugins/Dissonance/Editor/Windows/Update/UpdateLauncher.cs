using System;
using System.IO;
using System.Text;
using Dissonance.Editor.Windows.Welcome;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0618

namespace Dissonance.Editor.Windows.Update
{
    [InitializeOnLoad]
    public class UpdateLauncher
    {
        private const string CheckForNewVersionKey = "placeholder_dissonance_update_checkforlatestversion";
        private const string UpdaterToggleMenuItemPath = "Window/Dissonance/Check For Updates";

        private static readonly string StatePath = Path.Combine(DissonanceRootPath.BaseResourcePath, ".UpdateState.json");

        private static UnityWebRequest _request;

        /// <summary>
        /// This method will run as soon as the editor is loaded (with Dissonance in the project)
        /// </summary>
        static UpdateLauncher()
        {
            //Launching the window here caused some issues (presumably it's a bit too early for Unity to handle). Instead we'll wait until the first update call to do it.
            EditorApplication.update += Update;
        }

        [MenuItem(UpdaterToggleMenuItemPath, priority = 500)]
        public static void ToggleUpdateCheck()
        {
            var enabled = GetUpdaterEnabled();
            SetUpdaterEnabled(!enabled);
        }

        private static void Update()
        {
            //Reapply the current updater state to make sure everything is up to date
            UpdateMenuItemToggle();

            //Exit if the update check is not enabled
            if (!GetUpdaterEnabled())
            {
                Stop();
                return;
            }

            //Begin downloading the manifest of all Dissonance updates
            if (_request == null)
            {
                _request = UnityWebRequest.Get(string.Format("https://placeholder-software.co.uk/dissonance/releases/latest-published.html{0}", EditorMetadata.GetQueryString()));
                _request.Send();
                return;
            }

            //If we encounter an error just give up, the notification isn't critical
            if (_request.isNetworkError)
            {
                _request.Dispose();
                _request = null;
                Stop();
                return;
            }

            //Wait until the request is done
            if (!_request.isDone)
                return;

            //Get the bytes and dispose the request
            var bytes = _request.downloadHandler.data;
            _request.Dispose();
            _request = null;

            //Parse the response data. If we fail just give up, the notification isn't critical
            SemanticVersion latest;
            if (!TryParse(bytes, out latest) || latest == null)
            {
                Stop();
                return;
            }

            var state = GetUpdateState();

            //Check if we've already shown the window for a greater version
            if (latest.CompareTo(state.ShownForVersion) <= 0)
            {
                Stop();
                return;
            }

            //Check if the new version is greater than the currently installed version
            if (latest.CompareTo(WelcomeLauncher.CurrentDissonanceVersion) <= 0)
            {
                Stop();
                return;
            }

            //Update the state so that the window does not show up again for this version
            SetUpdateState(new UpdateState(latest));
            UpdateWindow.Show(latest, WelcomeLauncher.CurrentDissonanceVersion);
            Stop();
        }

        private static void Stop()
        {
            // We only want to run this once, so unsubscribe from update now that it has run
            // ReSharper disable once DelegateSubtraction (Justification: I know what I'm doing... famous last words)
            EditorApplication.update -= Update;
        }

        private static bool TryParse(byte[] bytes, [CanBeNull] out SemanticVersion parsed)
        {
            try
            {
                // The received data is a root level array. Wrap it in an object which gives the root array a name
                var str = Encoding.UTF8.GetString(bytes);
                parsed = JsonUtility.FromJson<SemanticVersion>(str);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                parsed = null;
                return false;
            }
        }

        private static UpdateState GetUpdateState()
        {
            if (!File.Exists(StatePath))
            {
                // State path does not exist at all so create the default
                var state = new UpdateState(new SemanticVersion());
                SetUpdateState(state);
                return state;
            }
            else
            {
                //Read the state from the file
                using (var reader = File.OpenText(StatePath))
                    return JsonUtility.FromJson<UpdateState>(reader.ReadToEnd());
            }
        }

        private static void SetUpdateState([CanBeNull] UpdateState state)
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

        internal static void SetUpdaterEnabled(bool enabled)
        {
            EditorPrefs.SetBool(CheckForNewVersionKey, enabled);
            UpdateMenuItemToggle();
        }

        private static void UpdateMenuItemToggle()
        {
            Menu.SetChecked(UpdaterToggleMenuItemPath, GetUpdaterEnabled());
        }

        internal static bool GetUpdaterEnabled()
        {
            if (!EditorPrefs.HasKey(CheckForNewVersionKey))
                return true;

            return EditorPrefs.GetBool(CheckForNewVersionKey);
        }

        [Serializable] private class UpdateState
        {
            [SerializeField] private SemanticVersion _shownForVersion;

            public SemanticVersion ShownForVersion
            {
                get { return _shownForVersion; }
            }

            public UpdateState(SemanticVersion version)
            {
                _shownForVersion = version;
            }

            public override string ToString()
            {
                return _shownForVersion.ToString();
            }
        }
    }
}