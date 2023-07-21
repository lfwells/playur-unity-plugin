using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
///<summary>Used in the Unity inspector to refer to either a legacy text box <see cref="UnityEngine.UI.Text"/> or a Text Mesh Pro text box <see cref="TMPro.TMP_Text"/></summary>
public class TextOrTMP 
{
    public Text unityText;
    public TMPro.TMP_Text TMP_Text;
    public string text
    {
        get {
            if (unityText != null) return unityText.text;
            if (TMP_Text != null) return TMP_Text.text;
            return string.Empty;
        }
        set {
            if (unityText != null) unityText.text = value;
            if (TMP_Text != null) TMP_Text.text = value;
        }
    }
}

[System.Serializable]
///<summary>Used in the Unity inspector to refer to either a legacy text field <see cref="UnityEngine.UI.InputField"/> or a Text Mesh Pro text field <see cref="TMPro.TMP_InputField"/></summary>
public class InputFieldOrTMP 
{
    public InputField unityText;
    public TMPro.TMP_InputField TMP_Text;
    public string text
    {
        get {
            if (unityText != null) return unityText.text;
            if (TMP_Text != null) return TMP_Text.text;
            return string.Empty;
        }
        set {
            if (unityText != null) unityText.text = value;
            if (TMP_Text != null) TMP_Text.text = value;
        }
    }
}

