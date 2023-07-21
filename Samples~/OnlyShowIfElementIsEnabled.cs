using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR
{
    /// <summary>
    /// A useful MonoBehaviour which (on startup) sets a gameobject active or inactive depending on the existence of the
    /// referenced <see cref="elementToCheck"/>. If the <see cref="Element"/> is not enabled for this user, then the object is 
    /// set to inactive.
    /// </summary>
    public class OnlyShowIfElementIsEnabled : MonoBehaviour
    {
        public Element elementToCheck; 

        void Start()
        {
            gameObject.SetActive(false);
            PlayURPlugin.instance.OnReady.AddListener(() =>
            {
                if (PlayURPlugin.instance.ElementEnabled(elementToCheck))
                    gameObject.SetActive(true);
                else
                    gameObject.SetActive(false);
            });
        }
    }
}