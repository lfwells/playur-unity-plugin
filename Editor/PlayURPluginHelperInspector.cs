using PlayUR;
using UnityEditor;
using UnityEditor.SearchService;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayURButtonInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Open PlayUR Settings"))
        {
            //open the unity project settings page for PlayUR
            SettingsService.OpenProjectSettings("Project/PlayUR");
        }
        if (GUILayout.Button("Open PlayUR Dashboard"))
        {
            Application.OpenURL(PlayURPlugin.DASHBOARD_URL);
        }
        //DrawDefaultInspector();

    }
}

[CustomEditor(typeof(PlayUR.PlayURPluginHelper))]
public class PlayURPluginHelperInspector : PlayURButtonInspector
{

}

[CustomEditor(typeof(PlayUR.PlayURBehaviour),true)]
public class PlayURBehaviourInspector : PlayURButtonInspector
{
    
}