using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayUR.Samples
{
    /// <summary>
    /// Sample MonoBehaviour which automatically populates a <see cref="Text"/> with the value of the parameter defined by <see cref="paramKey"/>.
    /// Shows a <see cref="defaultString"/> until the plugin is Ready (<see cref="PlayURPlugin.Ready"/>). 
    /// Doesn't handle <see cref="ParameterNotFoundException"/>.
    /// </summary>
    public class TestPluginText : MonoBehaviour
    {
        /// <summary>
        /// The key of the parameter defined on the back-end for the parameter.
        /// </summary>
        public string paramKey = "testKey";

        /// <summary>
        /// The text to show while waiting for the parameter to be loaded.
        /// </summary>
        public string defaultString = "";

        void Start()
        {
            GetComponent<Text>().text = defaultString;
            PlayURPlugin.instance.OnReady.AddListener(() => GetComponent<Text>().text = PlayURPlugin.instance.GetStringParam(paramKey));
        }
    }
}