using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlayUR
{
    public class PlayURSettings : ScriptableObject
    {
        public const string ResourcePath = "PlayURSettings";
        public const string SettingsPath = "Assets/PlayURPlugin/Resources/"+ResourcePath+".asset";

        /// <summary>
        /// Allows users to still use the PlayUR platform without having to connect to the PlayUR Dashboard. Some features may not be fully supported in this mode.
        /// </summary>
        public bool detachedMode = false;

        [SerializeField]
        private int gameId;
        /// <summary>
        /// The associated game ID on the PlayUR Dashboard
        /// </summary>
        public int GameID => gameId;

        /// <summary>
        /// this automagically creates a session handler (using <see cref="PlayURSessionTracker"/>), otherwise if false need to manually called StartSession etc
        /// </summary>
        public bool standardSessionTracking = true;

        [System.Serializable]
        /// <summary>
        /// Should the app start in full screen, start windowed, or remember previous run?
        /// </summary>
        public enum FullScreenStartUpMode
        {
            RememberPreviousState,
            AlwaysStartInFullScreen,
            AlwaysStartWindowed
        }
        /// <summary>
        /// Should the app start in full screen, start windowed, or remember previous run?
        /// </summary>
        public FullScreenStartUpMode fullScreenMode = FullScreenStartUpMode.RememberPreviousState;

        /// <summary>
        /// Override the PlayUR Platform's automatic choosing of an experiment on mobile builds by forcing the use of <see cref="mobileExperiment"/>
        /// </summary>
        public bool useSpecificExperimentForMobileBuild = false;

        /// <summary>
        /// The experiment to choose if <see cref="useSpecificExperimentForMobileBuild"/> is true.
        /// </summary>
        public Experiment mobileExperiment;

        /// <summary>
        /// Override the PlayUR Platform's automatic choosing of an experiment on desktop builds by forcing the use of <see cref="desktopExperiment"/>
        /// </summary>
        public bool useSpecificExperimentForDesktopBuild = false;

        /// <summary>
        /// The experiment to choose if <see cref="useSpecificExperimentForDesktopBuild"/> is true.
        /// </summary>
        public Experiment desktopExperiment;

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they start the game as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string mTurkStartMessage = "HiT Started";

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they complete the game as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string mTurkCompletionMessage = "HiT Completed\nCode: 6226";

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they start the game as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string prolificStartMessage = "Task Started";

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they complete the game as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string prolificCompletionMessage = "Task Completed";

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they copy the completion code as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string mTurkCompletionCodeCopiedMessage = "Submission Code Copied";

        [TextArea(3, 3)]
        /// <summary>
        /// What message should be shown to the user when they copy the completion code as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string prolificCompletionCodeCopiedMessage = "Submission Code Copied";

        /// <summary>
        /// For use in-editor only, this allows us to test the game with the Experiment defined in <see cref="experimentToTestInEditor"/>.
        /// </summary>
        public bool forceToUseSpecificExperiment = false;

        /// <summary>
        /// For use in-editor only, this allows us to test the game with a specific <see cref="Experiment"/>.
        /// </summary>
        public Experiment experimentToTestInEditor;

        /// <summary>
        /// For use in-editor only, this allows us to test the game with the ExperimentGroup defined in <see cref="groupToTestInEditor"/>.
        /// </summary>
        public bool forceToUseSpecificGroup = false;

        /// <summary>
        /// For use in-editor only, this allows us to test the game with a specific <see cref="ExperimentGroup"/>.
        /// </summary>
        public ExperimentGroup groupToTestInEditor;

        /// <summary>
        /// For use in-editor only, this allows us to test the game with a given Amazon Mechanical Turk ID.
        /// </summary>
        public string forceMTurkIDInEditor = null;

        /// <summary>
        /// For use in-editor only, this allows us to test the game with a given Prolific ID.
        /// </summary>
        public string forceProlificIDInEditor = null;

        /// <summary>
        /// The prefab to use that represents the highscore table. You can link this to the pre-made prefab in the PlayUR/HighScores folder, or create your own.
        /// </summary>
        public GameObject defaultHighScoreTablePrefab;

        /// <summary>
        /// The prefab to use that represents a dialog popup. You can link this to the pre-made prefab in the PlayUR folder, or create your own.
        /// </summary>
        public GameObject defaultPopupPrefab;

        /// <summary>
        /// The prefab to use that represents a survey popup window. You can link this to the pre-made prefab in the PlayUR/Survey folder, or create your own.
        /// </summary>
        public GameObject defaultSurveyPopupPrefab;

        /// <summary>
        /// The prefab to use that represents a survey row item. You can link this to the pre-made prefab in the PlayUR/Survey folder, or create your own.
        /// </summary>
        public GameObject defaultSurveyRowPrefab;

        /// <summary>
        /// The sprite asset representing the Amazon Mechanical Turk Logo. You can link this to the logo in the PlayUR/Murk folder.
        /// </summary>
        public Sprite mTurkLogo;

        /// <summary>
        /// The sprite asset representing the Prolific Logo. You can link this to the logo in the PlayUR/Prolific folder.
        /// </summary>
        public Sprite prolificLogo;

        /// <summary>
        /// The minimum log level to store in the PlayUR Platform. This is useful if you want to ignore certain log messages.
        /// </summary>
        public PlayURPlugin.LogLevel minimumLogLevelToStore = PlayURPlugin.LogLevel.Log;

        /// <summary>
        /// The minimum level to log to the console.
        /// </summary>
        public PlayURPlugin.LogLevel logLevel = PlayURPlugin.LogLevel.Log;

        #region Editor Overrides
        /// <summary>
        /// A set of element overrides that apply to the editor only. This allows you to test the game with different elements in the editor, without using the PlayUR back-end.
        /// </summary>
        public List<ElementOverride> editorElementOverrides = new List<ElementOverride>();

        /// <summary>
        /// A set of parameter overrides that apply to the editor only. This allows you to test the game with different parameters in the editor, without using the PlayUR back-end.
        /// </summary>
        public List<ParameterOverride> editorParameterOverrides = new List<ParameterOverride>();
        #endregion

#if UNITY_EDITOR
        internal static PlayURSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayURSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayURSettings>();
                settings.gameId = 0;
                settings.minimumLogLevelToStore = PlayURPlugin.LogLevel.Log;
                settings.logLevel = PlayURPlugin.LogLevel.Log;

                var runtimeFolder = Path.Combine("Packages","io.playur.unity", "Runtime");
                settings.defaultHighScoreTablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(runtimeFolder, "HighScores", "HighScoreTable.prefab"));
                settings.defaultPopupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(runtimeFolder, "Popups", "PopupCanvas.prefab"));
                settings.defaultSurveyPopupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(runtimeFolder, "Survey", "SurveyPopupPrefab.prefab"));
                settings.defaultSurveyRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(runtimeFolder, "Survey", "SurveyRowPrefab.prefab"));
                settings.mTurkLogo = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(runtimeFolder, "MTurk", "mturk.png"));
                settings.prolificLogo = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(runtimeFolder, "Prolific", "prolific.png"));

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

    [System.Serializable]
    public struct ElementOverride
    {
        public Element element;
        public bool overrideValue;
    }

    [System.Serializable]
    public struct ParameterOverride
    {
        public string parameter;
        public string overrideValue;
    }
}