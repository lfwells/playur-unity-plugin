using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayUR.Core;

namespace PlayUR
{
    /// <summary>
    /// This class goes on a prefab which is popped into any scene which you might want to immediately run in the editor from.
    /// Having this prefab in your scene will mean that on entering play mode the game will automatically redirect to the login scene and attempt to auto-login.
    /// </summary>
    public class PlayURPluginHelper : MonoBehaviour
    {
        [Header("Options")]
        /// <summary>
        /// this automagically creates a session handler (using <see cref="PlayURSessionTracker"/>), otherwise if false need to manually called StartSession etc
        /// </summary>
        public bool standardSessionTracking = true;


        [Header("Standalone -- Android and iOS (Note these get reset on Plugin Update!)")]
        /// <summary>
        /// Override the PlayUR Platform's automatic choosing of an experiment on mobile builds by forcing the use of <see cref="mobileExperiment"/>
        /// </summary>
        public bool useSpecificExperimentForMobileBuild = false;

        /// <summary>
        /// The experiment to choose if <see cref="useSpecificExperimentForMobileBuild"/> is true.
        /// </summary>
        public Experiment mobileExperiment;


        [Header("Standalone -- Windows, MacOS, Linux (Note these get reset on Plugin Update!)")]
        /// <summary>
        /// Override the PlayUR Platform's automatic choosing of an experiment on desktop builds by forcing the use of <see cref="desktopExperiment"/>
        /// </summary>
        public bool useSpecificExperimentForDesktopBuild = false;

        /// <summary>
        /// The experiment to choose if <see cref="useSpecificExperimentForDesktopBuild"/> is true.
        /// </summary>
        public Experiment desktopExperiment;


        [Header("Logging")]
        /// <summary>
        /// The minimum log level to store in the PlayUR Platform. This is useful if you want to ignore certain log messages.
        /// </summary>
        public PlayURPlugin.LogLevel minimumLogLevelToStore = PlayURPlugin.LogLevel.Log;


        [Header("MTurk Options (if used)")]
        [TextArea(3,3)]
        /// <summary>
        /// What message should be shown to the user when they complete the game as a Amazon Mechanical Turk user. \
        /// This value can (and should) be set at any time.
        /// </summary>
        public string mTurkCompletionMessage = "HiT Completed\nCode: 6226";
        
        [Header("Editor-Only Testing")]
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


        [Header("Prefabs and Assets")]
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


        public static PlayURPluginHelper instance;

        //used by the editor only
        public static int startedFromScene = -1;

        #region Domain Reloading Fix
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitDomainReload()
        {
            Debug.Log("Counter reset.");
            instance = null;
            startedFromScene = -1;
        }
        #endregion


        /// <summary>
        /// Includes a variant on the singleton pattern, begins session tracking if we are logged in.
        /// Otherwise kicks off the login process in-editor
        /// </summary>
        private void Awake()
        {
            //when we return the scene, a new plugin helper will appear, it should die straight away else we loop!
            if (instance != null)
            {
                DestroyImmediate(this.gameObject);
                return;
            }

            gameObject.AddComponent<URLParameters>();

            if (standardSessionTracking)
            {
                gameObject.AddComponent<PlayURSessionTracker>();
            }

            instance = this;
            DontDestroyOnLoad(this);
            StartCoroutine(Init());
        }
 
        /// <summary>
        /// Takes us to the login scene and returns us to the scene we came from as required.
        /// In builds, we will always get to the login scene first, then head on to Scene #1.
        /// </summary>
        IEnumerator Init()
        {
            //store current scene so we can return later
            var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            startedFromScene = currentSceneIndex;

            //if already on the login page, can ignore
            if (currentSceneIndex == 0)
            {
                startedFromScene = 0;
                yield break;
            }

            //login (if required...)
            while (PlayURLoginCanvas.LoggedIn == false)
            {
                yield return PlayURPlugin.GetLogin();
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            //definitely load the scene we were in in unity
            if (SceneManager.GetActiveScene().buildIndex != currentSceneIndex)
            {
                SceneManager.LoadScene(currentSceneIndex);
            }

                

            //no longer need this object
            //DestroyImmediate(this.gameObject);
        }
    }
}