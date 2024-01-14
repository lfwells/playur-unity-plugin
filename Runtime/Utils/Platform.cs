namespace PlayUR
{ 
    /// <summary>
    /// Defines on what platform the game was run, while also distinguishing between WebGL and Standalone
    /// </summary>
    public enum Platform
    { 
        Editor = -1,
        WebGL = 0,
        Windows = 2,
        MacOS = 3,
        Android = 4,
    }

    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        public Platform CurrentPlatform
        {
            get
            {
#if UNITY_EDITOR
                return Platform.Editor;
#elif UNITY_WEBGL
                return Platform.WebGL;
#elif UNITY_STANDALONE_WIN
                return Platform.Windows;
#elif UNITY_STANDALONE_OSX
                return Platform.MacOS;
#elif UNITY_ANDROID
                return Platform.Android;
#else
                return Platform.Editor;
#endif
            }
        }
    }
}