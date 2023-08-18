using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Samples
{
    /// <summary>
    /// Example code showing how to record <see cref="Action"/>s with and without extra meta data.
    /// </summary>
    public class AnalyticsCustom : PlayURBehaviour
    {
        /// <summary>
        /// The action to trigger when a collision happens
        /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
        /// </summary>
        public Action bounceAction;

        /// <summary>
        /// The anayltics column to store metadata (time) in.
        /// Note, no default value set as <see cref="Action"/> enum values may become invalid.
        /// </summary>
        public AnalyticsColumn column;

        public void OnCollisionEnter(Collision collision)
        {
            //set up the action to record but don't send it yet
            var a = PlayURPlugin.instance.BeginRecordAction(bounceAction);

            //add meta data to the action (this can happen immediately, or at any time as long as you have the reference to the action
            a.AddColumn(column, Time.time);

            //when ready, actually queue it up to record
            a.Record();
        }

        //some extra stuff for fun physics:
        public Vector2 initialVelocity;
        public override void Start()
        {
            base.Start();

            var rb = GetComponent<Rigidbody2D>();
            if (rb)
            {
                rb.velocity = initialVelocity;
            }
        }
    }
}
