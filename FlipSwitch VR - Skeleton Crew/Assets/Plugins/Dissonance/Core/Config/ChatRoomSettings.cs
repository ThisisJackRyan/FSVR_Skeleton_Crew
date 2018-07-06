using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Dissonance.Config
{
    public class ChatRoomSettings
        : ScriptableObject
    {
        private const string SettingsFileResourceName = "ChatRoomSettings";
        public static readonly string SettingsFilePath = Path.Combine(DissonanceRootPath.BaseResourcePath, SettingsFileResourceName + ".asset");
        private static readonly List<string> DefaultRooms = new List<string> { "Global", "Red Team", "Blue Team" };

        public List<string> Names;

        public ChatRoomSettings()
        {
            Names = new List<string>(DefaultRooms);
        }

        public static ChatRoomSettings Load()
        {
            return Resources.Load<ChatRoomSettings>(SettingsFileResourceName) ?? CreateInstance<ChatRoomSettings>();
        }
    }
}