using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlayUR
{
    //separate class for this, so that it doesn't get committed to source control
    public class PlayURClientSecretSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayURClientSecret";
        public const string SettingsPath = "Assets/PlayURPlugin/Resources/" + ResourcePath + ".asset";

        [SerializeField]
        private string clientSecret;

        public string ClientSecret => clientSecret;

#if UNITY_EDITOR
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
#endif
    }
}