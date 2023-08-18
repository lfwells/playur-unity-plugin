using System.Collections;
using PlayUR;
using TMPro;

/// <summary>
/// This sample allows you to save some text entered in a text box. On load, the text box will contain the last saved message in the box. 
/// PlayUR overrides PlayerPrefs to accomplish this.
/// </summary>
public class SavedGamesSample : PlayURBehaviour
{
    /// <summary>
    /// The input text box that stores loaded text, and is used for setting text to save
    /// </summary>
    public TMP_InputField input;

    /// <summary>
    /// The PlayerPrefs key that stores the saved value for this sample
    /// </summary>
    public string saveKey;

    /// <summary>
    /// The value to load if the saveKey is not found
    /// </summary>
    public string defaultValue;

    public override void OnReady()
    {
        input.text = PlayerPrefs.GetString(saveKey, defaultValue);
    }

    public void Save()
    {
        PlayerPrefs.SetString(saveKey, input.text);

        //note: no real need to call save on PlayerPrefs class, as it perioidcally saves (every 5 seconds)
        //PlayerPrefs.Save();
    }
}
