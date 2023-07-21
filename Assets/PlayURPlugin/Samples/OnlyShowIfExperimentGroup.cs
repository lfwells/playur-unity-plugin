using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// A useful MonoBehaviour which (on startup) sets a gameobject active or inactive depending on the current <see cref="ExperimentGroup"/>.
    /// </summary>
    public class OnlyShowIfExperimentGroup : MonoBehaviour
    {
        public ExperimentGroup experimentGroupToCheck; 

        void Start()
        {
            gameObject.SetActive(false);
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                if (PlayURPlugin.instance.CurrentExperimentGroup == experimentGroupToCheck)
                    gameObject.SetActive(true);
                else
                    gameObject.SetActive(false);
            });
        }
    }
}