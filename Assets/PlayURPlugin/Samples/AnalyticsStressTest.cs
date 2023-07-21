using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayUR;

public class AnalyticsStressTest : MonoBehaviour
{
    public PlayUR.Action actionToSpam;
    public PlayUR.AnalyticsColumn columnA;
    public PlayUR.AnalyticsColumn columnB;
    static int num;
    //send 100 in sequence actions per button click
    public void Send100()
    {
        StartCoroutine(Send100OncePerFrame());
        return;
    }
    public IEnumerator Send100OncePerFrame()
    {
        for (var i = 0; i < 100; i++)
        {
            //PlayURPlugin.instance.RecordAction(actionToSpam, num++);
            
            var a = PlayURPlugin.instance.BeginRecordAction(actionToSpam);
            //a.AddColumn(columnA, custom1.text+num);
            a.AddColumn(columnB, custom2.text+num++);
            a.Record();
            yield return new WaitForEndOfFrame();
        }
    }

    public InputField custom1, custom2;
    public void SendCustom()
    {
        var a = PlayURPlugin.instance.BeginRecordAction(actionToSpam);
        a.AddColumn(columnA, custom1.text);
        a.AddColumn(columnB, custom2.text);
        a.Record();
    }
}
