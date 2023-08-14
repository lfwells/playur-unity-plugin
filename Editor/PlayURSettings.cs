using UnityEditor;
using UnityEngine;

class PlayURSettings : ScriptableObject
{
    public const string k_MyCustomSettingsPath = "Assets/PlayURPlugin/PlayURSettings.asset";

    [SerializeField]
    private int gameId;

    [SerializeField]
    private string clientSecret;

    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public LogLevel logLevel;

    internal static PlayURSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PlayURSettings>(k_MyCustomSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PlayURSettings>();
            settings.gameId = 0;
            settings.clientSecret = "";
            settings.logLevel = LogLevel.Info;
            AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
            AssetDatabase.SaveAssets();
        }
        return settings;
    }

    internal static SerializedObject GetSerializedSettings()
    {
        return new SerializedObject(GetOrCreateSettings());
    }
}