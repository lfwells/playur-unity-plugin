using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayUR;

public class TestPlugin : MonoBehaviour {

    // dont want to have to do it this way
    /*
     * IEnumerator Start ()
    {
        while (PlayURPlugin.instance.Ready == false)
            yield return new WaitForEndOfFrame();

        Test();

        //testing exceptions
        //Debug.Log(PlayURPlugin.instance.GetFloatParam("testBool"));
        //Debug.Log(PlayURPlugin.instance.GetFloatParam("nothing"));
    }*/
    private void Start()
    {
        Test();
    }

    void Test()
    {
        Debug.Log(PlayURPlugin.instance.CurrentExperiment);
        Debug.Log(PlayURPlugin.instance.CurrentExperimentGroup);

        Debug.Log(PlayURPlugin.instance.ElementEnabled(Element.AchievementSystem));
        try
        {
            Debug.Log(PlayURPlugin.instance.GetStringParam("testKey2"));
            Debug.Log(PlayURPlugin.instance.GetBoolParam("testBool"));
        }
        catch (PlayUR.Exceptions.ParameterNotFoundException e)
        {
            Debug.LogError(e.Message);
        }
    }
}
