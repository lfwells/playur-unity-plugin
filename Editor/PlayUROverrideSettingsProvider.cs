using System.Linq;
using UnityEditor;
using UnityEngine;
using PlayUR.Editor;
using System.Collections.Generic;

namespace PlayUR
{
    //TODO: something smart editor-wise for particular parameter types where we know the type
    public class PlayUROverrideSettingsProvider : SettingsProvider
    {
        /// <summary>Current PlayUR settings</summary>
        private SerializedObject playurSettings;
        private SerializedProperty overrideParametersProperty, overrideElementsProperty;

        //TODO reimplement keywaords
        public PlayUROverrideSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope)
        {
            this.keywords = new string[0];// GetSearchKeywordsFromGUIContentProperties<Labels>();
        }

        /// <summary>Shows or Hides the settings based of the return value</summary>
        public static bool IsSettingsAvailable() => true;


        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            // Load the default settings (or create a new one) when the settings window is first clicked.
            playurSettings = PlayURSettings.GetSerializedSettings();
            overrideParametersProperty = playurSettings.FindProperty("editorParameterOverrides");
            overrideElementsProperty = playurSettings.FindProperty("editorElementOverrides");
        }

        public override void OnGUI(string searchContext)
        {
            if (playurSettings.targetObject == null) { Debug.Log("u wat?"); OnActivate(null, null); return; }

            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;

            // Game Configuration
            EditorGUI.indentLevel = 0;
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Apply overrides to elements and parameters that only take effect in the editor.\n\nUseful for testing the effect of different values", EditorStyles.helpBox);
            EditorGUILayout.PropertyField(overrideElementsProperty);
            EditorGUILayout.PropertyField(overrideParametersProperty);
            EditorGUI.indentLevel = 0;
            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = originalLabelWidth;

            playurSettings.ApplyModifiedProperties();
        }

        /// <summary>Initializes and creates the setting provider</summary>
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (!IsSettingsAvailable())
                return null;

            // Create a provider and automatically pull out the keywords
            return new PlayUROverrideSettingsProvider("Project/PlayUR/Editor Overrides", SettingsScope.Project);
        }


    }
}