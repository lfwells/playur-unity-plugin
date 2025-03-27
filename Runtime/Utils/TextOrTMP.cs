using TMPro;
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

//an extension on a gameObject to do a GetComponent, but it will look for either a Text or a TextMeshPro component
public static class TMPOrTextExtension
{
    /// <summary>
    /// Tries to get a Text or TextMeshPro component from a GameObject and set the text on it.
    /// </summary>
    /// <returns>If a text or a text mesh pro object was found</returns>
    public static bool TryGetTextComponentAndSetText(this GameObject obj, string text, bool inChildren = true)
    {
        if (inChildren)
        {
            var textComponent = obj.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                return true;
            }
            var tmpComponent = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                tmpComponent.text = text;
                return true;
            }
        }
        else
        {
            var textComponent = obj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                return true;
            }
            var tmpComponent = obj.GetComponent<TextMeshProUGUI>();
            if (tmpComponent != null)
            {
                tmpComponent.text = text;
                return true;
            }
        }
        return false;

    }
}