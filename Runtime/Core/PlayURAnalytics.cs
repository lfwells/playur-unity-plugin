using System;
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
        public struct ActionParams
        {
            public Action a;
            public string timestamp;
            public string extra;
            //extra analytics columns (new version)
            public List<int> includedColumns;
            public List<string> columnValues; 
        }
        [System.Serializable]
        public struct ActionParamsList //weirdly to serialize to json, we need a struct to wrap the array
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
            actionQueue.Enqueue(GenerateUploadableActionData(a,extra,HTMLencode,debugOutput,waitForPendingActions,columns));
        }

        IEnumerator RecordActionDirectly(ActionParams singleAction, Rest.ServerCallback callback)
        {
            yield return StartCoroutine(RecordActionDirectly(new ActionParamsList { actions = new ActionParams[] { singleAction } }, callback));
        }
        IEnumerator RecordActionDirectly(ActionParamsList actions, Rest.ServerCallback callback)
        {
            if (IsDetachedMode)
            {
                yield return StartCoroutine(DetachedModeProxy.RecordActionDirectly(this, actions, callback));
                yield break;
            }

            var form = Rest.GetWWWFormWithExperimentInfo();
            form.Add("actions", JsonUtility.ToJson(actions));

            while (!inSession) yield return new WaitForEndOfFrame();
            form.Add("sessionID", sessionID.ToString());

            yield return StartCoroutine(Rest.EnqueuePost("UserAction", form, HTMLencode: false, debugOutput: true, callback: callback));
        }
        ActionParams GenerateUploadableActionData(Action a, object extra = null, bool HTMLencode = false, bool debugOutput = false, bool waitForPendingActions = true, Dictionary<AnalyticsColumn, object> columns = null)
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

            return parameters;
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

                ActionParamsList actions = new ActionParamsList()
                {
                    actions = new ActionParams[actionQueue.Count]
                };
                actions.actions = actionQueue.ToArray();
                actionQueue.Clear();
                Log(JsonUtility.ToJson(actions));

                //pendingActions = true;
                yield return StartCoroutine(RecordActionDirectly(actions, callback: null));
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

            internal Dictionary<AnalyticsColumn, object> columns = new Dictionary<AnalyticsColumn, object>();

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
                var extras = GetExtras();
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

            /// <summary>
            /// Converts the columns for this action into a list of strings in order (used internally)
            /// </summary>
            /// <returns></returns>
            internal List<string> GetExtras()
            {
                var extras = new List<string>();
                foreach (var column in PlayURPlugin.instance.configuration.analyticsColumnsOrder)
                {
                    if (columns.ContainsKey(column) && columns[column] != null)
                        extras.Add(columns[column].ToString());
                }
                return extras;
            }

            internal string GetExtra(AnalyticsColumn column)
            {
                if (columns.ContainsKey(column) == false) return null;
                return columns[column].ToString();
            }
        }

        protected ActionParams GenerateUploadableActionData(InProgressAction a, bool HTMLencode = false, bool debugOutput = false, bool waitForPendingActions = true)
        {
            return PlayURPlugin.instance.GenerateUploadableActionData(a.action, string.Join(",", a.GetExtras()), HTMLencode: HTMLencode, debugOutput: debugOutput, waitForPendingActions: waitForPendingActions, columns: a.columns);
        }
        #endregion

        #region Updatable Actions
        IEnumerator UpdatableActionsRoutine()
        {
            while (true)
            {
                foreach (var a in updatableActions)
                {
                    yield return StartCoroutine(a.Process());
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        List<IProcessAction> updatableActions = new();

        public interface IProcessAction
        {
            public IEnumerator Process();   
        }
        /// <summary>
        /// Class to wrap an analytics action that can be updated over time. Can be updated frequently, and is updated to the server at a regular interval.
        /// </summary>
        /// <typeparam name="T">The type of data to record in the action. Should be a primative.</typeparam>
        public class UpdatableAction<T> : IProcessAction
        {
            protected InProgressAction action;

            bool initialUploadComplete = false;
            Dictionary<AnalyticsColumn, bool> dirty = new Dictionary<AnalyticsColumn, bool>();
            float lastUpdate = -1f;
            int userActionID = -1;

            /// <summary>
            /// Data will only be sent to server this number of seconds since the last upload has completed. However if data has not updated, no additional upload will take place.
            /// </summary>
            public float uploadIntervalSeconds = 5f;

            /// <summary>
            /// Create an action that can be updated over time.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            public UpdatableAction(Action action)
            {
                this.action = instance.BeginRecordAction(action);
                instance.updatableActions.Add(this);

                var uploadData = instance.GenerateUploadableActionData(this.action);
                instance.StartCoroutine(instance.RecordActionDirectly(uploadData, (bool succ, JSONNode result) => {
                    if (succ)
                    {
                        initialUploadComplete = true;
                        userActionID = result["id"];
                    }
                }));
            }
            /// <summary>
            /// Create an action that can be updated over time, and set the initial data.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            /// <param name="column">The column to set initial data for</param>
            /// <param name="initialData">The initial data to put into the column</param>
            public UpdatableAction(Action action, AnalyticsColumn column, T initialData)
            {
                this.action = instance.BeginRecordAction(action);
                instance.updatableActions.Add(this);

                var uploadData = instance.GenerateUploadableActionData(this.action);
                instance.StartCoroutine(instance.RecordActionDirectly(uploadData, (bool succ, JSONNode result) => {
                    if (succ)
                    {
                        initialUploadComplete = true;
                        userActionID = result["id"];

                        Set(column, initialData);
                    }
                }));

            }

            /// <summary>
            /// Update this action's data for the given column
            /// </summary>
            /// <param name="column">The column to change data for</param>
            /// <param name="data">The value to set the column to (overrides original data, creates the column if it didn't previoulsy exist)</param>
            /// <returns></returns>
            public InProgressAction Set(AnalyticsColumn column, T data)
            { 
                this.action.UpdateColumn(column, data);

                if (this.dirty.ContainsKey(column) == false)
                {
                    this.dirty.Add(column, true);
                }
                this.dirty[column] = true;

                return this.action;
            }

            /// <summary>
            /// Run the actual upload. Used internally.
            /// </summary>
            /// <returns></returns>
            public IEnumerator Process()
            {
                if (PlayURPlugin.instance.IsDetachedMode)
                {
                    yield return instance.StartCoroutine(PlayURPlugin.instance.DetachedModeProxy.ProcessUpdatableAction(this));
                    yield break;
                }

                if (Time.realtimeSinceStartup - lastUpdate > uploadIntervalSeconds)
                {
                    if (initialUploadComplete == false)
                    {
                        yield break;
                    }

                    lastUpdate = Time.realtimeSinceStartup;
                    foreach (var kvp in dirty.ToArray())
                    {
                        if (kvp.Value == true)//is dirty
                        {
                            var column = kvp.Key;
                            dirty[column] = false;


                            var form = Rest.GetWWWFormWithExperimentInfo();
                            form.Add("userActionID", userActionID.ToString());
                            form.Add("columnID", ((int)column).ToString());
                            form.Add("value", action.GetExtra(column)?.ToString() ?? "");

                            yield return instance.StartCoroutine(Rest.EnqueuePut("UserActionExtraColumn", -1, form, HTMLencode: false, debugOutput: false));
                        }
                    }
                }

            }
            
        }
        /// <summary>
        /// An action that can have data for a given column appended-to over time. Can be updated frequently, and is updated to the server at a regular interval.
        /// </summary>
        public class AppendableAction : UpdatableAction<string>
        {
            protected string currentDataValue = "";
            protected AnalyticsColumn column;

            /// <summary>
            /// The current value stored in the column for this action. 
            /// </summary>
            public string Value { get { return currentDataValue; } }

            /// <summary>
            /// Create an action that can be appended-to over time.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            /// <param name="column">The column to use for storing the data</param>
            public AppendableAction(Action action, AnalyticsColumn column) : base(action) { this.column = column; }

            /// <summary>
            /// Create an action that can be appended-to over time, and set the initial data.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            /// <param name="column">The column to use for storing the data</param>
            /// <param name="initialData">The initial data to store in the column</param>
            public AppendableAction(Action action, AnalyticsColumn column, string initialData) : base(action, column, initialData) { this.column = column; }

            /// <summary>
            /// Append data to the column for this action. Is not immediately uploaded to server, PlayUR handles this on a regular interval instead.
            /// </summary>
            /// <param name="data"></param>
            /// <returns></returns>
            public InProgressAction Append(string data)
            {
                this.currentDataValue += data;
                return Set(this.column, this.currentDataValue);
            }
        }

        /// <summary>
        /// An action that can have data for a given column appended-to over time. Can be updated frequently, and is updated to the server at a regular interval.
        /// </summary>
        public class CountAction : UpdatableAction<int>
        {
            protected int currentDataValue = 0;
            protected AnalyticsColumn column;

            /// <summary>
            /// The current value stored in the column for this action. 
            /// </summary>
            public int Value { get { return currentDataValue; } }

            /// <summary>
            /// Create an action that can be appended-to over time.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            /// <param name="column">The column to use for storing the data</param>
            public CountAction(Action action, AnalyticsColumn column) : base(action) { this.column = column; }

            /// <summary>
            /// Create an action that can be appended-to over time, and set the initial data.
            /// </summary>
            /// <param name="action">The analytics action id to tag this action with</param>
            /// <param name="column">The column to use for storing the data</param>
            /// <param name="initialData">The initial data to store in the column</param>
            public CountAction(Action action, AnalyticsColumn column, int initialData) : base(action, column, initialData) { this.column = column; }

            /// <summary>
            /// Add to the count for the column for this action. Is not immediately uploaded to server, PlayUR handles this on a regular interval instead.
            /// </summary>
            /// <param name="by">The amount to increment by</param>
            /// <returns></returns>
            public InProgressAction Increment(int by = 1)
            {
                this.currentDataValue += by;
                return Set(this.column, this.currentDataValue);
            }
        }
        #endregion
    }
}