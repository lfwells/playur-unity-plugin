using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// A useful MonoBehaviour which (on startup) sets a gameobject active or inactive depending on the current <see cref="Experiment"/>.
    /// </summary>
    public class OnlyShowIfExperiment : MonoBehaviour
    {
        public Experiment experimentToCheck; 

        void Start()
        {
            gameObject.SetActive(false);
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                if (PlayURPlugin.instance.CurrentExperiment == experimentToCheck)
                    gameObject.SetActive(true);
                else
                    gameObject.SetActive(false);
            });
        }
    }
}