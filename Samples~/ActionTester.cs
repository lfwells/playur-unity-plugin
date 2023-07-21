using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Samples
{
    /// <summary>
    /// Example code showing how to record <see cref="Action"/>s with and without extra meta data.
    /// </summary>
    public class ActionTester : MonoBehaviour
    {
        /// <summary>
        /// The action to trigger when the user clicks (left or right, extra meta data stores which)
        /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
        /// </summary>
        public Action clickAction; 

        /// <summary>
        /// The action to trigger when the game is won (in this case by pressing space once :P)
        /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
        /// </summary>
        public Action winGameAction;

        void Update()
        {
            //Record a simple action (no extra metadata)
            /*
             * 
             * 
public class AnalyticsColumns : Dictionary<PlayUR.AnalyticsColumn, object> { }
             * 
             * 
             * PlayURPlugin.instance.RecordAction(Action.Attack);

            //Record a player action (with extra metadata)
            PlayURPlugin.instance.RecordAction(Action.Jump, new AnalyticsColumns{
                { AnalyticsColumn.Type, "Double" }
            });
            */
            if (Input.GetMouseButtonDown(0))
                PlayURPlugin.instance.RecordAction(clickAction, "left click");

            if (Input.GetMouseButtonDown(1))
                PlayURPlugin.instance.RecordAction(clickAction, "right click");

            if (Input.GetKeyDown(KeyCode.Space))
                PlayURPlugin.instance.RecordAction(winGameAction);
            
        }
    }
}
