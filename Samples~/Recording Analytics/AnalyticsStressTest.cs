using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PlayUR;

public class AnalyticsStressTest : PlayURBehaviour
{
    /// <summary>
    /// The analytics action to create 100 of.
    /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
    /// </summary>
    public Action actionToSpam;

    /// <summary>
    /// The anayltics column to store metadata in.
    /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
    /// </summary>
    public AnalyticsColumn column;

    /// <summary>
    /// Send 100 analytics actions over the course of the next 100 frames (stress test)
    /// </summary>
    public void Send100()
    {
        if (!PlayURPlugin.instance.IsReady) return;

        StartCoroutine(Send100OncePerFrame());
    }
    IEnumerator Send100OncePerFrame()
    {
        for (var i = 0; i < 100; i++)
        {
            //PlayURPlugin.instance.RecordAction(actionToSpam, num++);
            
            var a = PlayURPlugin.instance.BeginRecordAction(actionToSpam);
            a.AddColumn(column, "Frame:"+Time.frameCount);
            a.Record();
            yield return new WaitForEndOfFrame();
        }
    }
}
