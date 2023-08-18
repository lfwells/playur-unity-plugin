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
    public class OnlyShowIfElementIsEnabled : PlayURBehaviour
    {
        public Element elementToCheck;

        public override void Start()
        {
            base.Start();

            //ensure the object is not visible on startup
            gameObject.SetActive(false);
        }
        public override void OnReady()
        {
            base.OnReady();
            if (PlayURPlugin.instance.ElementEnabled(elementToCheck))
                gameObject.SetActive(true);
            else
                gameObject.SetActive(false);
        }
    }
}