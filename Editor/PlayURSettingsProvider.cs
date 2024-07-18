using System.Linq;
using UnityEditor;
using UnityEngine;
using PlayUR.Editor;
using System.Collections.Generic;
using PlayUR.Core;
using Unity.EditorCoroutines.Editor;

namespace PlayUR
{
    public class PlayURSettingsProvider : SettingsProvider
    {
        /// <summary>Current PlayUR settings</summary>
        private SerializedObject playurSettings;
        private SerializedProperty gameIdProperty;
        private SerializedProperty standardSessionTracking;
        private SerializedProperty fullScreenMode;
        private SerializedProperty useSpecificExperimentForMobileBuild;
        private SerializedProperty mobileExperiment;
        private SerializedProperty useSpecificExperimentForDesktopBuild;
        private SerializedProperty desktopExperiment;
        private SerializedProperty mTurkStartMessage;
        private SerializedProperty mTurkCompletionMessage;
        private SerializedProperty mTurkCompletionCodeCopiedMessage;
        private SerializedProperty prolificStartMessage;
        private SerializedProperty prolificCompletionMessage;
        private SerializedProperty prolificCompletionCodeCopiedMessage;
        private SerializedProperty forceToUseSpecificExperiment;
        private SerializedProperty experimentToTestInEditor;
        private SerializedProperty forceToUseSpecificGroup;
        private SerializedProperty groupToTestInEditor;
        private SerializedProperty forceMTurkIDInEditor;
        private SerializedProperty forceProlificIDInEditor;
        private SerializedProperty defaultHighScoreTablePrefab;
        private SerializedProperty defaultPopupPrefab;
        private SerializedProperty defaultSurveyPopupPrefab;
        private SerializedProperty defaultSurveyRowPrefab;
        private SerializedProperty mTurkLogo;
        private SerializedProperty prolificLogo;
        private SerializedProperty logLevel;
        private SerializedProperty logLevelToStore;

        private SerializedObject playurClientSecretSettings;
        private SerializedProperty clientSecretProperty;

        private bool _foldout_checks
        {
            get { return EditorPrefs.GetBool("_foldout_checks", true); }
            set { EditorPrefs.SetBool("_foldout_checks", value); }
        }

        private bool _foldout_config
        {
            get { return EditorPrefs.GetBool("_foldout_config", true); }
            set { EditorPrefs.SetBool("_foldout_config", value); }
        }
        private bool _foldout_general
        {
            get { return EditorPrefs.GetBool("_foldout_general", false); }
            set { EditorPrefs.SetBool("_foldout_general", value); }
        }
        private bool _foldout_mturk
        {
            get { return EditorPrefs.GetBool("_foldout_mturk", false); }
            set { EditorPrefs.SetBool("_foldout_mturk", value); }
        }
        private bool _foldout_prolific
        {
            get { return EditorPrefs.GetBool("_foldout_prolific", false); }
            set { EditorPrefs.SetBool("_foldout_prolific", value); }
        }
        private bool _foldout_editorSettings
        {
            get { return EditorPrefs.GetBool("_foldout_editorSettings", false); }
            set { EditorPrefs.SetBool("_foldout_editorSettings", value); }
        }
        private bool _foldout_mobileDesktopSettings
        {
            get { return EditorPrefs.GetBool("_foldout_mobileDesktopSettings", false); }
            set { EditorPrefs.SetBool("_foldout_mobileDesktopSettings", value); }
        }
        private bool _foldout_prefabs
        {
            get { return EditorPrefs.GetBool("_foldout_prefabs", false); }
            set { EditorPrefs.SetBool("_foldout_prefabs", value); }
        }

        private bool _foldout_enums
        {
            get { return EditorPrefs.GetBool("_foldout_enums", true); }
            set { EditorPrefs.SetBool("_foldout_enums", value); }
        }

        private bool _foldout_parameters
        {
            get { return EditorPrefs.GetBool("_foldout_parameters", false); }
            set { EditorPrefs.SetBool("_foldout_parameters", value); }
        }
        private bool _foldout_elements
        {
            get { return EditorPrefs.GetBool("_elements_parameters", false); }
            set { EditorPrefs.SetBool("_elements_parameters", value); }
        }
        private bool _foldout_experiments
        {
            get { return EditorPrefs.GetBool("_foldout_experiments", false); }
            set { EditorPrefs.SetBool("_foldout_experiments", value); }
        }
        private bool _foldout_groups
        {
            get { return EditorPrefs.GetBool("_foldout_groups", false); }
            set { EditorPrefs.SetBool("_foldout_groups", value); }
        }
        private bool _foldout_actions
        {
            get { return EditorPrefs.GetBool("_foldout_actions", false); }
            set { EditorPrefs.SetBool("_foldout_actions", value); }
        }
        private bool _foldout_analytics
        {
            get { return EditorPrefs.GetBool("_foldout_analytics", false); }
            set { EditorPrefs.SetBool("_foldout_analytics", value); }
        }

        private string[] _parameters, _elements;

        private struct Labels
        {   // We define all the labels in a seperate class so we can automatically pull them as keywords

            public static GUIContent setUpChecks = new GUIContent("Setup Checks and Warnings");
            public static GUIContent gameIDSet = new GUIContent("Game ID Set");
            public static GUIContent clientSecretSet = new GUIContent("Client Secret Set");
            public static GUIContent buildSettingsSet = new GUIContent("Login Scene Set in Build Settings");
            public static GUIContent enumsGenerated = new GUIContent("Enums Generated");

            public static GUIContent fix = new GUIContent("Fix");
            public static GUIContent done = new GUIContent("Done");
            public static GUIContent fixBelow = new GUIContent("Fix Below");

            public static GUIContent gameConfiguration = new GUIContent("Configuration Settings");
            public static GUIContent gameId = new GUIContent("Game ID");
            public static GUIContent clientSecret = new GUIContent("Client Secret");

            public static GUIContent generalSettings = new GUIContent("General Settings");
            public static GUIContent standardSessionTracking = new GUIContent("Use Standard Session Tracking");
            public static GUIContent fullScreenMode = new GUIContent("Full Screen Mode");
            public static GUIContent logLevel = new GUIContent("Log Level");
            public static GUIContent logLevelToStore = new GUIContent("Storage Log Level");

            public static GUIContent mTurk = new GUIContent("MTurk Settings");
            public static GUIContent mTurkStartMessage = new GUIContent("MTurk Start Message");
            public static GUIContent mTurkCompletionMessage = new GUIContent("MTurk Completion Message");
            public static GUIContent mTurkCompletionCodeCopiedMessage = new GUIContent("MTurk Completion Code Copied Message");
            public static GUIContent mTurkSprite = new GUIContent("MTurk Sprite");
            public static GUIContent mTurkForceID = new GUIContent("Force MTurk ID in Editor");

            public static GUIContent prolific = new GUIContent("Prolific Settings");
            public static GUIContent prolificStartMessage = new GUIContent("Prolific Start Message");
            public static GUIContent prolificCompletionMessage = new GUIContent("Prolific Completion Message");
            public static GUIContent prolificCompletionCodeCopiedMessage = new GUIContent("Prolific Completion Code Copied Message");
            public static GUIContent prolificSprite = new GUIContent("Prolific Sprite");
            public static GUIContent prolificForceID = new GUIContent("Force Prolific ID in Editor");

            public static GUIContent editorSettings = new GUIContent("Editor Settings");
            public static GUIContent forceExperiment = new GUIContent("Force Experiment in Editor");
            public static GUIContent experiment = new GUIContent("Experiment");
            public static GUIContent forceExperimentGroup = new GUIContent("Force Experiment Group in Editor");
            public static GUIContent experimentGroup = new GUIContent("Experiment Group");

            public static GUIContent mobileAndDesktop = new GUIContent("Mobile and Desktop Settings");
            public static GUIContent mobileExpForce = new GUIContent("Force Experiment on Mobile");
            public static GUIContent desktopExpForce = new GUIContent("Force Experiment on Desktop");

            public static GUIContent prefabsSettings = new GUIContent("Prefab Settings");
            public static GUIContent prefabHighScoreTable = new GUIContent("High Score Table");
            public static GUIContent prefabPopup = new GUIContent("Popup");
            public static GUIContent prefabSurveyPopup = new GUIContent("Survey Popup");
            public static GUIContent prefabSurveyRow = new GUIContent("Survey Row");

            public static GUIContent enums = new GUIContent("Debug PlayUR Configuration");
            public static GUIContent generateEnums = new GUIContent("Generate Enums");

            public static GUIContent elements = new GUIContent("Elements");
            public static GUIContent parameters = new GUIContent("Parameters");
            public static GUIContent experiments = new GUIContent("Experiments");
            public static GUIContent groups = new GUIContent("Groups");
            public static GUIContent actions = new GUIContent("Analytics Actions");
            public static GUIContent analyticColumns = new GUIContent("Analytics Columns");
        }

        public PlayURSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            this.keywords = GetSearchKeywordsFromGUIContentProperties<Labels>();
        }

        /// <summary>Shows or Hides the settings based of the return value</summary>
        public static bool IsSettingsAvailable() => true;

        string[] warnings = new string[0];

        private string[] GetPlayURParameters()
        {
            if (_parameters != null) return _parameters;

            _parameters = typeof(PlayUR.Parameter)
                .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .Select(field => field.Name)
                .ToArray();
            return _parameters;
        }

        private string[] GetPlayURElements()
        {
            if (_elements != null) return _elements;

            _elements = typeof(PlayUR.Element)
                .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .Select(field => field.Name)
                .ToArray();
            return _elements;
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            // Load the default settings (or create a new one) when the settings window is first clicked.
            playurSettings = PlayURSettings.GetSerializedSettings();
            gameIdProperty = playurSettings.FindProperty("gameId");
            standardSessionTracking = playurSettings.FindProperty("standardSessionTracking");
            fullScreenMode = playurSettings.FindProperty("fullScreenMode");
            useSpecificExperimentForMobileBuild = playurSettings.FindProperty("useSpecificExperimentForMobileBuild");
            mobileExperiment = playurSettings.FindProperty("mobileExperiment");
            useSpecificExperimentForDesktopBuild = playurSettings.FindProperty("useSpecificExperimentForDesktopBuild");
            desktopExperiment = playurSettings.FindProperty("desktopExperiment");
            mTurkStartMessage = playurSettings.FindProperty("mTurkStartMessage");
            mTurkCompletionMessage = playurSettings.FindProperty("mTurkCompletionMessage");
            mTurkCompletionCodeCopiedMessage = playurSettings.FindProperty("mTurkCompletionCodeCopiedMessage");
            prolificStartMessage = playurSettings.FindProperty("prolificStartMessage");
            prolificCompletionMessage = playurSettings.FindProperty("prolificCompletionMessage");
            prolificCompletionCodeCopiedMessage = playurSettings.FindProperty("prolificCompletionCodeCopiedMessage");
            forceToUseSpecificExperiment = playurSettings.FindProperty("forceToUseSpecificExperiment");
            experimentToTestInEditor = playurSettings.FindProperty("experimentToTestInEditor");
            forceToUseSpecificGroup = playurSettings.FindProperty("forceToUseSpecificGroup");
            groupToTestInEditor = playurSettings.FindProperty("groupToTestInEditor");
            defaultHighScoreTablePrefab = playurSettings.FindProperty("defaultHighScoreTablePrefab");
            defaultPopupPrefab = playurSettings.FindProperty("defaultPopupPrefab");
            defaultSurveyPopupPrefab = playurSettings.FindProperty("defaultSurveyPopupPrefab");
            defaultSurveyRowPrefab = playurSettings.FindProperty("defaultSurveyRowPrefab");
            forceMTurkIDInEditor = playurSettings.FindProperty("forceMTurkIDInEditor");
            mTurkLogo = playurSettings.FindProperty("mTurkLogo");
            forceProlificIDInEditor = playurSettings.FindProperty("forceProlificIDInEditor");
            prolificLogo = playurSettings.FindProperty("prolificLogo");
            logLevel = playurSettings.FindProperty("logLevel");
            logLevelToStore = playurSettings.FindProperty("minimumLogLevelToStore");

            playurClientSecretSettings = PlayURClientSecretSettings.GetSerializedSettings();
            clientSecretProperty = playurClientSecretSettings.FindProperty("clientSecret");

            //PlayURPluginEditor.CheckForUpdates();
            PlayURPluginEditor.GetCurrentVersion();

            PlayURPluginEditor.CheckForWarnings(gameIdProperty.intValue, w => warnings = w);
        }

        public override void OnGUI(string searchContext)
        {
            if (playurSettings.targetObject == null) { Debug.Log("u wat?"); OnActivate(null, null); return; }
            if (playurClientSecretSettings.targetObject == null) { OnActivate(null, null); return; }

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;

            var redStyle = new GUIStyle(EditorStyles.label);
            redStyle.normal.textColor = Color.red;
            var greenStyle = new GUIStyle(EditorStyles.label);
            greenStyle.normal.textColor = Color.green;
            var yellowStyle = new GUIStyle(EditorStyles.label);
            yellowStyle.normal.textColor = Color.yellow;
            var redButton = new GUIStyle(EditorStyles.iconButton);
            redButton.normal.textColor = Color.red;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"Plugin Version: {PlayURPluginEditor.currentVersion}");
            if (PlayURPluginEditor.UpdateAvailable == null)
            {
                if (PlayURPluginEditor.checkingForUpdate)
                {
                    GUILayout.Label("Checking...");
                }
                else if (GUILayout.Button("Check for Update"))
                {
                    PlayURPluginEditor.CheckForUpdates();
                }
            }
            else if (PlayURPluginEditor.UpdateAvailable == true)
            {
                var oldColor = GUI.color;
                GUI.color = Color.red;
                if (GUILayout.Button($"Update Available: Version: {PlayURPluginEditor.latestVersion}"))
                {
                    PlayUREditorUtils.OpenPackageManager();
                }
                GUI.color = oldColor;
            }
            else
            {
                GUILayout.Label("Plugin is at Latest Version", greenStyle);
            }
            EditorGUILayout.EndHorizontal();


            // Set-Up Checks
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_checks = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_checks, Labels.setUpChecks);
            if (_foldout_checks)
            {
                EditorGUI.indentLevel = 1;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Labels.gameIDSet);
                if (gameIdProperty.intValue > 0)
                    EditorGUILayout.LabelField(Labels.done, greenStyle);
                else
                    EditorGUILayout.LabelField(Labels.fixBelow, redStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Labels.clientSecret);
                if (!string.IsNullOrEmpty(clientSecretProperty.stringValue))
                    EditorGUILayout.LabelField(Labels.done, greenStyle);
                else
                    EditorGUILayout.LabelField(Labels.fixBelow, redStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Labels.enumsGenerated);
                if (System.Enum.GetValues(typeof(Experiment)).Length > 0)
                    EditorGUILayout.LabelField(Labels.done, greenStyle);
                else
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.red;
                    if (GUILayout.Button(Labels.fix))
                    {
                        PlayURPluginEditor.GenerateEnum();
                    }
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Labels.buildSettingsSet);
                var scenes = new List<EditorBuildSettingsScene>();
                scenes.AddRange(EditorBuildSettings.scenes);

                if (scenes.Where((scene) => scene.path.Contains("PlayURLogin")).Count() == 1)
                    EditorGUILayout.LabelField(Labels.done, greenStyle);
                else
                {
                    var oldColor = GUI.color;
                    GUI.color = Color.red;
                    if (GUILayout.Button(Labels.fix))
                    {
                        PlayURPluginEditor.SetSceneBuildSettings();
                    }
                    GUI.color = oldColor;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (warnings != null)
            {
                foreach (var warning in warnings)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent(warning), yellowStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();



            // Game Configuration
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_config = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_config, Labels.gameConfiguration);
            if (_foldout_config)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(gameIdProperty, Labels.gameId);
                EditorGUILayout.PropertyField(clientSecretProperty, Labels.clientSecret);
                if (GUILayout.Button("Open Game Config on Dashboard"))
                {
                    Application.OpenURL(PlayURPlugin.DASHBOARD_URL + "Game/" + gameIdProperty.intValue);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            // Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_general = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_general, Labels.generalSettings);
            if (_foldout_general)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(standardSessionTracking, Labels.standardSessionTracking);
                EditorGUILayout.PropertyField(fullScreenMode, Labels.fullScreenMode);
                EditorGUILayout.PropertyField(logLevelToStore, Labels.logLevelToStore);
                EditorGUILayout.PropertyField(logLevel, Labels.logLevel);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();


            // Editor Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_editorSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_editorSettings, Labels.editorSettings);
            if (_foldout_editorSettings)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(forceToUseSpecificExperiment, Labels.forceExperiment);
                if (forceToUseSpecificExperiment.boolValue)
                {
                    EditorGUILayout.PropertyField(experimentToTestInEditor, Labels.experiment);
                }
                EditorGUILayout.PropertyField(forceToUseSpecificGroup, Labels.forceExperimentGroup);
                if (forceToUseSpecificGroup.boolValue)
                {
                    EditorGUILayout.PropertyField(groupToTestInEditor, Labels.experimentGroup);
                }

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            // Mobile and Desktop Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_mobileDesktopSettings = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_mobileDesktopSettings, Labels.mobileAndDesktop);
            if (_foldout_mobileDesktopSettings)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(useSpecificExperimentForMobileBuild, Labels.mobileExpForce);
                if (useSpecificExperimentForMobileBuild.boolValue)
                {
                    EditorGUILayout.PropertyField(mobileExperiment, Labels.experiment);
                }
                EditorGUILayout.PropertyField(useSpecificExperimentForDesktopBuild, Labels.desktopExpForce);
                if (useSpecificExperimentForDesktopBuild.boolValue)
                {
                    EditorGUILayout.PropertyField(desktopExperiment, Labels.experimentGroup);
                }

            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();


            // Prefabs
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_prefabs = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_prefabs, Labels.prefabsSettings);
            if (_foldout_prefabs)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(defaultHighScoreTablePrefab, Labels.prefabHighScoreTable);
                EditorGUILayout.PropertyField(defaultPopupPrefab, Labels.prefabPopup);
                EditorGUILayout.PropertyField(defaultSurveyPopupPrefab, Labels.prefabSurveyPopup);
                EditorGUILayout.PropertyField(defaultSurveyRowPrefab, Labels.prefabSurveyRow);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();


            // MTurk
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_mturk = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_mturk, Labels.mTurk);
            if (_foldout_mturk)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(mTurkStartMessage, Labels.mTurkStartMessage);
                EditorGUILayout.PropertyField(mTurkCompletionMessage, Labels.mTurkCompletionMessage);
                EditorGUILayout.PropertyField(mTurkCompletionCodeCopiedMessage, Labels.mTurkCompletionCodeCopiedMessage);
                EditorGUILayout.PropertyField(forceMTurkIDInEditor, Labels.mTurkForceID);
                EditorGUILayout.PropertyField(mTurkLogo, Labels.mTurkSprite);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            // Prolific
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_prolific = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_prolific, Labels.prolific);
            if (_foldout_prolific)
            {
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(prolificStartMessage, Labels.prolificStartMessage);
                EditorGUILayout.PropertyField(prolificCompletionMessage, Labels.prolificCompletionMessage);
                EditorGUILayout.PropertyField(prolificCompletionCodeCopiedMessage, Labels.prolificCompletionCodeCopiedMessage);
                EditorGUILayout.PropertyField(forceProlificIDInEditor, Labels.prolificForceID);
                EditorGUILayout.PropertyField(prolificLogo, Labels.prolificSprite);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();


            // ActionsEditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            _foldout_enums = EditorGUILayout.BeginFoldoutHeaderGroup(_foldout_enums, Labels.enums);
            if (_foldout_enums)
            {
                EditorGUI.indentLevel = 1;
                if (GUILayout.Button(Labels.generateEnums, EditorStyles.miniButton))
                {
                    PlayURPluginEditor.GenerateEnum();
                    _parameters = null;
                }

                EditorGUILayout.Space();
                _foldout_experiments = EnumNameFoldout<PlayUR.Experiment>(_foldout_experiments, Labels.experiments);
                _foldout_groups = GroupListItemFoldout(_foldout_groups, Labels.groups);

                EditorGUILayout.Space();
                GUILayout.Label("Previewing Values for Experiment: " + (previewGroup?.ToString() ?? "NONE" + (loadingConfig ? "(Loading...)" : "")), EditorStyles.boldLabel);
                _foldout_elements = ElementListFoldout(_foldout_elements, GetPlayURElements(), Labels.elements);
                _foldout_parameters = ParameterListFoldout(_foldout_parameters, GetPlayURParameters(), Labels.parameters);

                EditorGUILayout.Space();
                _foldout_actions = EnumNameFoldout<PlayUR.Action>(_foldout_actions, Labels.actions);
                EditorGUILayout.Space();
                _foldout_analytics = EnumNameFoldout<PlayUR.AnalyticsColumn>(_foldout_analytics, Labels.analyticColumns);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = originalLabelWidth;

            playurSettings.ApplyModifiedProperties();
            playurClientSecretSettings.ApplyModifiedProperties();
        }

        /// <summary>Initializes and creates the setting provider</summary>
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (!IsSettingsAvailable())
                return null;

            // Create a provider and automatically pull out the keywords
            return new PlayURSettingsProvider("Project/PlayUR", SettingsScope.Project);
        }

        private bool EnumNameFoldout<T>(bool foldout, GUIContent label)
            where T : System.Enum => StringListFoldout(foldout, System.Enum.GetNames(typeof(T)), label);


        class CoroutineRunner { }
        private bool GroupListItemFoldout(bool foldout, GUIContent label)
        {
            var values = System.Enum.GetNames(typeof(ExperimentGroup));
            //foldout = EditorGUILayout.Foldout(foldout, label + " (" + values.Length + ")");
            GUILayout.Label(label + " (" + values.Length + ")", EditorStyles.boldLabel);
            foldout = true;
            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                EditorGUILayout.BeginVertical();
                foreach (var name in values)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(name, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(15));
                    if (GUILayout.Button("Preview Values", EditorStyles.miniButton, GUILayout.Width(150)))
                    {
                        previewGroup = (ExperimentGroup)System.Enum.Parse(typeof(ExperimentGroup), name);


                        PlayURPlugin.Log("Getting Configuration...");
                        loadingConfig = true;
                        var form = Rest.GetWWWForm();
                        form.Add("experimentGroupID", ((int)previewGroup.Value).ToString());

                        var runner = new CoroutineRunner();
                        EditorCoroutineUtility.StartCoroutine(Rest.Get("Configuration/debug.php", form, (succ, result) => {
                            configuration = PlayURPlugin.ParseConfigurationResult(succ, result);
                            loadingConfig = false;
                        }, debugOutput: true), runner);

                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel = 1;
            }
            return foldout;
        }

        public ExperimentGroup? previewGroup;
        public Configuration configuration;
        public bool loadingConfig = false;

        private bool StringListFoldout(bool foldout, string[] values, GUIContent label)
        {
            //foldout = EditorGUILayout.Foldout(foldout, label + " (" + values.Length + ")");
            GUILayout.Label(label + " (" + values.Length + ")", EditorStyles.boldLabel);
            foldout = true;
            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                EditorGUILayout.BeginVertical();
                foreach (var name in values)
                {
                    EditorGUILayout.SelectableLabel(name, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(15));
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel = 1;
            }
            return foldout;
        }



        private bool ParameterListFoldout(bool foldout, string[] values, GUIContent label)
        {
            //foldout = EditorGUILayout.Foldout(foldout, label + " (" + values.Length + ")");
            GUILayout.Label(label + " (" + values.Length + ")", EditorStyles.boldLabel);
            foldout = true;
            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                EditorGUILayout.BeginVertical();
                foreach (var name in values)
                {
                    var value = loadingConfig ? "" : "NOT SET";
                    configuration?.parameters.TryGetValue(name, out value);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(name, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(15), GUILayout.Width(150));
                    if (previewGroup != null)
                        GUILayout.Label(value, GUILayout.MaxHeight(15));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel = 1;
            }
            return foldout;
        }

        private bool ElementListFoldout(bool foldout, string[] values, GUIContent label)
        {
            //foldout = EditorGUILayout.Foldout(foldout, label + " (" + values.Length + ")");
            GUILayout.Label(label + " (" + values.Length + ")", EditorStyles.boldLabel);
            foldout = true;
            if (foldout)
            {
                EditorGUI.indentLevel = 2;
                EditorGUILayout.BeginVertical();
                foreach (var name in values)
                {
                    var value = configuration?.elements.FindIndex(e => e == System.Enum.Parse<Element>(name)) >= 0;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(name, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(15));
                    if (previewGroup != null)
                        GUILayout.Label(value ? "ENABLED" : "");
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel = 1;
            }
            return foldout;
        }


    }
}