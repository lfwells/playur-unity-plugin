using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlayUR
{
    public class PlayURSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayURSettings";
        public const string SettingsPath = "Assets/PlayURPlugin/Resources/"+ResourcePath+".asset";

        [SerializeField]
        private int gameId;

        public int GameID => gameId;


        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        public LogLevel logLevel;

        internal static PlayURSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayURSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayURSettings>();
                settings.gameId = 0;
                settings.logLevel = LogLevel.Info;

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }

    //separate class for this, so that it doesn't get committed to source control
    public class PlayURClientSecretSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayURClientSecret";
        public const string SettingsPath = "Assets/PlayURPlugin/Resources/" + ResourcePath + ".asset";

        [SerializeField]
        private string clientSecret;

        public string ClientSecret => clientSecret;

        internal static PlayURClientSecretSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayURClientSecretSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayURClientSecretSettings>();
                settings.clientSecret = "";

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
}