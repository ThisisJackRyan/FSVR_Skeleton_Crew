//using UnityEditor;
//using UnityEngine;
//using Object = UnityEngine.Object;

//namespace Dissonance.Editor
//{
//    public class DissonanceAecFilterInspector
//        : IAudioEffectPluginGUI
//    {
//        private bool _initialized;
//        private Texture2D _logo;

//        private void Initialize()
//        {
//            _logo = Resources.Load<Texture2D>("dissonance_logo");

//            _initialized = true;
//        }

//        public override bool OnGUI(IAudioEffectPlugin plugin)
//        {

//            if (!_initialized)
//                Initialize();


//            GUILayout.Label(_logo);

//            if (Application.isPlaying)
//            {
//                EditorGUILayout.LabelField("Sample Rate: " + plugin.GetSampleRate());

//                var comms = Object.FindObjectOfType<DissonanceComms>();
//                if (comms == null)
//                    return true;

//                var mic = comms.MicCapture;
//                if (mic == null)
//                    return true;

//                //var aec = mic.AEC;
//                //if (aec == null)
//                //    return true;

//                //float poor;
//                //bool farend;
//                //int median;
//                //int deviation;
//                //aec.GetStats(out farend, out median, out deviation, out poor);

//                //EditorGUILayout.LabelField(farend ? "Farend is ACTIVE" : "Farend is INACTIVE");
//                //EditorGUILayout.LabelField(string.Format("Delay: {0} median, {1} deviation", median, deviation));
//                //EditorGUILayout.LabelField(string.Format("Poor Fraction: {0}", poor));
//                //EditorGUILayout.LabelField("Amplitude Delta: " + aec.AmplitudeDrop.ToString("+#.###;-#.###;+0.000"));
//            }

//            return true;
//        }

//        public override string Name
//        {
//            get { return "Dissonance Echo Cancellation"; }
//        }

//        public override string Description
//        {
//            get { return "Captures audio for Dissonance Acoustic Echo Cancellation"; }
//        }

//        public override string Vendor
//        {
//            get { return "Placeholder Software"; }
//        }
//    }
//}
