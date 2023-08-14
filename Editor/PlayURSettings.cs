using UnityEditor;
using UnityEngine;

class PlayURSettings : ScriptableObject
{
    public const string k_MyCustomSettingsPath = "Assets/PlayURPlugin/PlayURSettings.asset";

    [SerializeField]
    private int gameId;


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

//separate class for this, so that it doesn't get committed to source control
class PlayURClientIDSettings : ScriptableObject
{
    public const string k_MyCustomSettingsPath = "Assets/PlayURPlugin/PlayURClientID.asset";
    [SerializeField]
    private string clientSecret;

    internal static PlayURClientIDSettings GetOrCreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<PlayURClientIDSettings>(k_MyCustomSettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PlayURClientIDSettings>();
            settings.clientSecret = "";
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