using System.ComponentModel;
using System.IO;
using Dissonance.Audio.Capture;
using UnityEngine;

namespace Dissonance.Config
{
    public sealed class VoiceSettings
        :
#if !NCRUNCH
        ScriptableObject,
#endif
        INotifyPropertyChanged
    {
        #region fields and properties
        private static readonly Log Log = Logs.Create(LogCategory.Recording, typeof(VoiceSettings).Name);

        // ReSharper disable InconsistentNaming
        private const string PersistName_Quality = "Dissonance_Audio_Quality";
        private const string PersistName_FrameSize = "Dissonance_Audio_FrameSize";

        private const string PersistName_DenoiseAmount = "Dissonance_Audio_Denoise_Amount";
        // ReSharper restore InconsistentNaming

        private const string SettingsFileResourceName = "VoiceSettings";
        public static readonly string SettingsFilePath = Path.Combine(DissonanceRootPath.BaseResourcePath, SettingsFileResourceName + ".asset");

        #region codec settings
        [SerializeField]private AudioQuality _quality;
        public AudioQuality Quality
        {
            get { return _quality; }
            set
            {
                Preferences.Set(PersistName_Quality, ref _quality, value, (key, q) => PlayerPrefs.SetInt(key, (int)q), Log, setAtRuntime: false);
                OnPropertyChanged("Quality");
            }
        }

        [SerializeField]private FrameSize _frameSize;
        public FrameSize FrameSize
        {
            get { return _frameSize; }
            set
            {
                Preferences.Set(PersistName_FrameSize, ref _frameSize, value, (key, f) => PlayerPrefs.SetInt(key, (int)f), Log, setAtRuntime: false);
                OnPropertyChanged("FrameSize");
            }
        }
        #endregion

        #region preprocessor settings
        [SerializeField]private int _denoiseAmount;
        public NoiseSuppressionLevels DenoiseAmount
        {
            get { return (NoiseSuppressionLevels)_denoiseAmount; }
            set
            {
                Preferences.Set(PersistName_DenoiseAmount, ref _denoiseAmount, (int)value, PlayerPrefs.SetInt, Log);
                OnPropertyChanged("DenoiseAmount");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private static VoiceSettings _instance;
        [NotNull] public static VoiceSettings Instance
        {
            get { return _instance ?? (_instance = Load()); }
        }
        #endregion

        public VoiceSettings()
        {
            _quality = AudioQuality.Medium;
            _frameSize = FrameSize.Medium;

            _denoiseAmount = (int)NoiseSuppressionLevels.High;
        }

        public static void Preload()
        {
            if (_instance == null)
                _instance = Load();
        }

        [NotNull] private static VoiceSettings Load()
        {
#if NCRUNCH
            return new VoiceSettings();
#else
            var settings = Resources.Load<VoiceSettings>(SettingsFileResourceName) ?? CreateInstance<VoiceSettings>();

            //Get all the settings values
            Preferences.Get(PersistName_Quality, ref settings._quality, (s, q) => (AudioQuality)PlayerPrefs.GetInt(s, (int)q), Log);
            Preferences.Get(PersistName_FrameSize, ref settings._frameSize, (s, f) => (FrameSize)PlayerPrefs.GetInt(s, (int)f), Log);

            Preferences.Get(PersistName_DenoiseAmount, ref settings._denoiseAmount, PlayerPrefs.GetInt, Log);

            return settings;
#endif
        }
    }
}
