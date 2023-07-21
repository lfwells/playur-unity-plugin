using UnityEngine;
using UnityEditor;
using System.IO;

public class PopupInput : EditorWindow
{
    public delegate void Callback(string input, bool cancelled);
    
    string inputVar;

    string message, button;
    Callback callback;
    
    public static void Open(string title, string message, Callback callback = null, string button = "OK", string defaultText = "")
    {
        PopupInput window = CreateInstance<PopupInput>();
        window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
        window.titleContent = new GUIContent(title);
        window.message = message;
        window.button = button;
        window.callback = callback;
        window.inputVar = defaultText;
        window.ShowUtility();
    }
    bool f = false;
    private void OnLostFocus()
    {
        this.Focus();
    }
    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField(message, EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        GUI.SetNextControlName("MyTextField");
        inputVar = EditorGUILayout.TextField(GUIContent.none, inputVar);
        GUILayout.Space(60);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(button) || Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
        {
            if (callback != null) callback(inputVar, cancelled:false);
            Close();return;
        }


        if (GUILayout.Button("Cancel"))
        {
            if (callback != null) callback(inputVar, cancelled: true);
            Close();return;
        }

        EditorGUILayout.EndHorizontal();

        if (f == false)
        {
            EditorGUI.FocusTextInControl("MyTextField");
            f = true;
        }
    }
}
