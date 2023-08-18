using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayUR;

public class GetDebugLogHistory : MonoBehaviour
{
    

    public void ButtonPressed()
    {
        GetComponent<Text>().text = PlayURPlugin.instance.GetDebugLogs(PlayURPlugin.Settings.minimumLogLevelToStore);
    }
}
