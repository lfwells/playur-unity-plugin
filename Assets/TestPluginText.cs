using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using PlayUR;

public class TestPluginText : MonoBehaviour {

    public string paramKey = "testKey";

    public void Start()
    {
        try
        {
            GetComponent<Text>().text = PlayURPlugin.instance.GetStringParam(paramKey);
        }
        catch (PlayUR.Exceptions.ParameterNotFoundException)
        {
            GetComponent<Text>().text = "Parameter not found";
        }
    }
}
