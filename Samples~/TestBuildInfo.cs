using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayUR.Samples
{
    /// <summary>
    /// Simple MonoBehaviour which displays the current build configuration (from <see cref="PlayUR.Configuration"/>) on a <see cref="Text"/>.
    /// </summary>
    public class TestBuildInfo : MonoBehaviour
    {
        void Start()
        {
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                GetComponent<Text>().text = $"Build ID: {PlayURPlugin.instance.CurrentBuildID} - Branch: {PlayURPlugin.instance.CurrentBuildBranch} ";
            });
        }
    }
}