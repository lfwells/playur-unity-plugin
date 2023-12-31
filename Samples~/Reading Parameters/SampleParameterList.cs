﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PlayUR.Samples
{
    /// <summary>
    /// Sample MonoBehaviour which automatically populates a <see cref="Text"/> with a random value from the parameter defined by <see cref="paramKey"/>.
    /// Shows a <see cref="defaultString"/> until the plugin is Ready (<see cref="PlayURPlugin.Ready"/>). 
    /// Doesn't handle <see cref="ParameterNotFoundException"/>.
    /// </summary>
    public class SampleParameterList : PlayURBehaviour
    {
        /// <summary>
        /// The key of the parameter defined on the back-end for the parameter.
        /// </summary>
        public string paramKey = "testListKey";

        /// <summary>
        /// The text to show while waiting for the parameter to be loaded.
        /// </summary>
        public string defaultString = "";

        public override void Start()
        {
            GetComponent<Text>().text = defaultString;
            PlayURPlugin.instance.OnReady.AddListener(GetNextRandomText);

            base.Start();
        }

        /// <summary>
        /// Get a random entry from the given list
        /// </summary>
        public void GetNextRandomText()
        {
            //good practice to check if system is ready or not
            if (!PlayURPlugin.instance.IsReady) return;

            string[] list = PlayURPlugin.instance.GetStringParamList(paramKey);
            GetComponent<Text>().text = "A random item from the parameters is: " + list[Random.Range(0, list.Length)];
        }
    }
}