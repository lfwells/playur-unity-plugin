using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlayUR
{
    public class PlayURSettingsProvider : SettingsProvider
    {
        /// <summary>Current PlayUR settings</summary>
        private SerializedObject playurSettings;
        private SerializedProperty gameIdProperty;
        private SerializedProperty logLevelProperty;

        private SerializedObject playurClientSecretSettings;
        private SerializedProperty clientSecretProperty;

        private bool _foldout_parameters = false;
        private bool _foldout_experiments = false;
        private bool _foldout_groups = false;
        private bool _foldout_analytics = false;

        private string[] _parameters;

        private struct Labels
        {   // We define all the labels in a seperate class so we can automatically pull them as keywords
            public static GUIContent gameConfiguration = new GUIContent("Game Setup");
            public static GUIContent gameId = new GUIContent("Game ID");
            public static GUIContent clientSecret = new GUIContent("Client Secret");
            public static GUIContent settings = new GUIContent("Settings");
            public static GUIContent logLevel = new GUIContent("Log Level");
            public static GUIContent enums = new GUIContent("Enums");
            public static GUIContent generateEnums = new GUIContent("Generate Enums");

            public static GUIContent parameters = new GUIContent("Parameters");
            public static GUIContent experiments = new GUIContent("Experiments");
            public static GUIContent groups = new GUIContent("Groups");
            public static GUIContent analyticColumns = new GUIContent("Analytic Columns");
        }

        public PlayURSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            this.keywords = GetSearchKeywordsFromGUIContentProperties<Labels>();
        }

        /// <summary>Shows or Hides the settings based of the return value</summary>
        public static bool IsSettingsAvailable() => true;

        private string[] GetPlayURParameters()
        {
            if (_parameters != null) return _parameters;

            _parameters = typeof(PlayUR.Parameter)
                .GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
                .Select(field => field.Name)
                .ToArray();
            return _parameters;
        }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            // Load the default settings (or create a new one) when the settings window is first clicked.
            playurSettings = PlayURSettings.GetSerializedSettings();
            gameIdProperty = playurSettings.FindProperty("gameId");
            logLevelProperty = playurSettings.FindProperty("logLevel");

            playurClientSecretSettings = PlayURClientSecretSettings.GetSerializedSettings();
            clientSecretProperty = playurClientSecretSettings.FindProperty("clientSecret");
        }

        public override void OnGUI(string searchContext)
        {
            // Game Configuration
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(Labels.gameConfiguration, EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(gameIdProperty, Labels.gameId);
                EditorGUILayout.PropertyField(clientSecretProperty, Labels.clientSecret);
            }
            EditorGUILayout.EndVertical();

            // Settings
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(Labels.settings, EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;
                EditorGUILayout.PropertyField(logLevelProperty, Labels.logLevel);
            }
            EditorGUILayout.EndVertical();

            // Actions
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label(Labels.enums, EditorStyles.boldLabel);
                EditorGUI.indentLevel = 1;
                if (GUILayout.Button(Labels.generateEnums, EditorStyles.miniButton))
                {
                    PlayUR.Editor.PlayURPluginEditor.GenerateEnum();
                    _parameters = null;
                }

                EditorGUILayout.Space();
                EnumNameFoldout<PlayUR.Experiment>(ref _foldout_experiments, Labels.experiments);
                EnumNameFoldout<PlayUR.ExperimentGroup>(ref _foldout_groups, Labels.groups);
                StringListFoldout(ref _foldout_parameters, GetPlayURParameters(), Labels.parameters);
                EnumNameFoldout<PlayUR.AnalyticsColumn>(ref _foldout_analytics, Labels.analyticColumns);
            }
            EditorGUILayout.EndVertical();

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

        private void EnumNameFoldout<T>(ref bool foldout, GUIContent label)
            where T : System.Enum => StringListFoldout(ref foldout, System.Enum.GetNames(typeof(T)), label);


        private void StringListFoldout(ref bool foldout, string[] values, GUIContent label)
        {
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label + " (" + values.Length + ")");
            if (foldout)
            {
                foreach (var name in values)
                {
                    EditorGUILayout.SelectableLabel(name, EditorStyles.wordWrappedMiniLabel, GUILayout.MaxHeight(15));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

    }
}