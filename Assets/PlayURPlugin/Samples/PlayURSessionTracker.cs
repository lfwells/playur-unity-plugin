using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// Automatic session tracker. Starts a session when the game starts, and ends it when the application is closed.
    /// You may want your own session logic, in which case don't use this MonoBehaviour.
    /// This functionality is enabled automatically if <see cref="PlayURPluginHelper.standardSessionTracking"/> value is true.
    /// In the future will contain functionality for detecting if the application has lost focus etc.
    /// </summary>
    public class PlayURSessionTracker : MonoBehaviour
    {
        void Start()
        {
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                PlayURPlugin.instance.StartSession();
            });
        }
        private void OnApplicationQuit()
        {
            PlayURPlugin.instance.EndSession();
            if (Core.Rest.Queue != null)
                Core.Rest.Queue.ProcessImmediate();
        }
    }
}