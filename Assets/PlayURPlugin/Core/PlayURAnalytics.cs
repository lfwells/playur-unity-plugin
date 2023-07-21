using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using PlayUR.Core;
using UnityEngine;

namespace PlayUR
{
    public partial class PlayURPlugin
    {
        [System.Serializable]
        protected struct ActionParams
        {
            public Action a;
            public string timestamp;
            public string extra;
            //extra analytics columns (new version)
            public List<int> includedColumns;
            public List<string> columnValues; 
        }
        [System.Serializable]
        protected struct ActionParamsList //weidly to serialize to json, we need a struct to wrap the array
        {
            public ActionParams[] actions;
        }
        Queue<ActionParams> actionQueue = new Queue<ActionParams>();
        //bool pendingActions = false; //TODO: unsure if we also might like a queue

        /// <summary>
        /// Record a single instance of an <see cref="Action"/> occuring.
        /// Note <see cref="Action"/> enum is enumerated in the editor to match actions defined on the server back-end.
        /// </summary>
        /// <param name="a">The action type to record, as defind on the PlayUR back-end.</param>
        /// <param name="extra">the additional meta data about this action (e.g. if end match, what was the score?)</param>
        /// <param name="HTMLencode">Optionally convert form items special characters using <code>WebUtility.HtmlEncode</code>. </param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. </param>
        /// <param name="waitForPendingActions">Optionally wait for any previous requests to complete before sending this one 
        ///(note, original timestamp will be preserved) -- defaults to true. </param>
        /// <param name="columns">Optionally any custom-set up columns and their data. For complex data, use <see cref="BeginRecordAction(Action)"/> instead.</param>
        public void RecordAction(Action a, object extra = null, bool HTMLencode = false, bool debugOutput = false, bool waitForPendingActions = true, Dictionary<AnalyticsColumn, object> columns = null)
        {
            var timestamp = GetMysqlFormatTime();

            var parameters = new ActionParams
            {
                a = a,
                timestamp = timestamp,
                extra = null,
            };

            var e = extra?.ToString();
            if (e != null && HTMLencode)
            {
                e = WebUtility.HtmlEncode(e);
                parameters.extra = e;
            }
            else if (e != null)
            {
                parameters.extra = e;
            }

            if (columns != null && columns.Count > 0)
            {
                parameters.includedColumns = new List<int>();
                parameters.columnValues = new List<string>();
                foreach (var column in configuration.analyticsColumnsOrder) //this ordered array ensures that we maintain correct order of columns
                {
                    if (columns.ContainsKey(column) && columns[column] != null)
                    {
                        parameters.includedColumns.Add((int)column);
                        var val = columns[column].ToString();
                        if (HTMLencode)
                            val = WebUtility.HtmlEncode(val);
                        parameters.columnValues.Add(val);
                    }
                }
            }

            //check to see if the 

            actionQueue.Enqueue(parameters);
            
            //StartCoroutine(RecordActionRoutine(a, timestamp, extra, HTMLencode, debugOutput, waitForPendingActions));
        }
        IEnumerator RecordActionRoutine()
        {
            while (true)
            {
                if (actionQueue.Count > 0)
                {
                    yield return StartCoroutine(RecordActionRoutineInternal());                    
                }
                else
                {
                    yield return new WaitForSecondsRealtime(1f);
                }
            }
        }
        IEnumerator RecordActionRoutineInternal()
        {
            if (actionQueue.Count > 0)
            {
                Log("Sending off "+actionQueue.Count+" actions in a batch");

                var form = Rest.GetWWWFormWithExperimentInfo();
                ActionParamsList actions = new ActionParamsList()
                {
                    actions = new ActionParams[actionQueue.Count]
                };
                actions.actions = actionQueue.ToArray();
                actionQueue.Clear();
                Log(JsonUtility.ToJson(actions));
                
                form.Add("actions", JsonUtility.ToJson(actions));
                //form.Add("actionID", ((int)a).ToString());
                //form.Add("timestamp", timestamp);
                //form.Add("experimentID", configuration.experimentID.ToString());
                //form.Add("experimentGroupID", configuration.experimentGroupID.ToString());
                //if (extra != null)
                    //form.Add("extra", extra.ToString());
                if (inSession)
                    form.Add("sessionID", sessionID.ToString());

                //pendingActions = true;
                yield return StartCoroutine(Rest.EnqueuePost("UserAction", form, HTMLencode: false, debugOutput: true));
            }
        }
        /// <summary>
        /// Record a single instance of an <see cref="Action"/> occuring.
        /// Note <see cref="Action"/> enum is enumerated in the editor to match actions defined on the server back-end.
        /// </summary>
        /// <param name="a">the action to record</param>
        /// <param name="HTMLencode">Optionally convert form items special characters using <code>WebUtility.HtmlEncode</code>. </param>
        /// <param name="extra">the additional meta data about this action (e.g. if end match, what was the score?) -- in this case an array of itmes which will be concatenated</param>
        public void RecordAction(Action a, bool HTMLencode = false, params object[] extra)
        {
            RecordAction(a, string.Join(",",extra.Select(o => o.ToString())), HTMLencode: HTMLencode);
        }

        #region More complex action recording
        /// <summary>
        /// For more complex analytics, you can use this method to record an action across multiple sections of code.
        /// This function creates a new <see cref="InProgressAction"/> which you can then build up by appending column data to it.
        /// Once you are done, call <see cref="InProgressAction.Record"/> to upload the action to the server.
        /// </summary>
        /// <param name="action">The action type to record, as defind on the PlayUR back-end.</param>
        /// <returns>The <see cref="InProgressAction"/> to be used throughout the code, for later uploading.</returns>
        public InProgressAction BeginRecordAction(Action action)
        {
            return new InProgressAction() { action = action };
        }

        /// <summary>
        /// Represents an an analytics action that is being built, to be uploaded to the server later on.
        /// Useful for recording information across time/sections of the code, and then uploading it all at once.
        /// </summary>
        public class InProgressAction
        {
            /// <summary>
            /// The Analytics action to record, as defined on the PlayUR back-end.
            /// </summary>
            public Action action;

            Dictionary<AnalyticsColumn, object> columns = new Dictionary<AnalyticsColumn, object>();

            /// <summary>
            /// Upload the action to the server.
            /// </summary>
            /// /// <param name="HTMLencode">Optionally convert form items special characters using <code>WebUtility.HtmlEncode</code>. </param>
            /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. </param>
            /// <param name="waitForPendingActions"></param>
            /// <param name="waitForPendingActions">Optionally wait for any previous requests to complete before sending this one 
            ///(note, original timestamp will be preserved) -- defaults to true. </param>
            public void Record(bool HTMLencode = false, bool debugOutput = false, bool waitForPendingActions = true)
            {
                var extras = new List<string>();
                foreach (var column in PlayURPlugin.instance.configuration.analyticsColumnsOrder)
                {
                    if (columns.ContainsKey(column) && columns[column] != null)
                        extras.Add(columns[column].ToString());
                }
                PlayURPlugin.instance.RecordAction(this.action, string.Join(",",extras), HTMLencode:HTMLencode, debugOutput:debugOutput, waitForPendingActions:waitForPendingActions, columns:columns);
            }

            /// <summary>
            /// Append information to this in-progress action
            /// </summary>
            /// <param name="column">The Analytics Column name for this data as defined on the PlayUR back-end.</param>
            /// <param name="value">The value for this column. Gets converted to a string upon upload.</param>
            public void AddColumn(AnalyticsColumn column, object value)
            {
                if (columns.ContainsKey(column) == false)
                    columns.Add(column, null);
                columns[column] = value;
            }
            /// <summary>
            /// Update information for this in-progress action. If the column doesn't exist, it will be created.
            /// Just an alias for <see cref="AddColumn"/>
            /// </summary>
            /// <param name="column">The Analytics Column name for this data as defined on the PlayUR back-end.</param>
            /// <param name="value">The value for this column. Gets converted to a string upon upload.</param>
            public void UpdateColumn(AnalyticsColumn column, object value)
            {
                AddColumn(column, value);
            }
        }
        #endregion
    }
}