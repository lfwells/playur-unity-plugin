using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Samples
{
    /// <summary>
    /// Example code showing how to record <see cref="Action"/>s in the most basic format.
    /// </summary>
    public class AnalyticsBasic : PlayURBehaviour
    {
        /// <summary>
        /// The analytics action to trigger when the user clicks (left or right, extra meta data stores which)
        /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
        /// </summary>
        public Action clickAction; 

        //to be linked up to a button
        public void OnButtonClick()
        { 
            PlayURPlugin.instance.RecordAction(clickAction);
        }
    }
}
