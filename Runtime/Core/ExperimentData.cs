using System.Collections;
using System.Collections.Generic;
using PlayUR.Core;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        /// <summary>
        /// Gets the experiment data for the specified key. Data is obtained from the local cache, which is loaded when game first starts. To enxure data is updated, call FetchExperimentData() to get the latest data from the server first.
        /// </summary>
        /// <param name="key">The key of the experiment data to retrieve.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <returns>The experiment data for the specified key, or the default value if the key is not found.</returns>
        public string GetExperimentData(string key, string defaultValue = "")
        {
            return Configuration.experimentData.TryGetValue(key, out var value) ? value : defaultValue;
        }
        /// <summary>
        /// Sets the experiment data for the specified key. Saves to the PlayUR server.
        /// </summary>
        /// <param name="key">The key of the experiment data to set.</param>
        /// <param name="value">The value to set for the experiment data.</param>
        public void SetExperimentData(string key, string value)
        {
            Configuration.experimentData[key] = value;

            var form = Rest.GetWWWFormWithExperimentInfo();
            form.Add("keyName", key);
            form.Add("value", value);

            if (Configuration.experimentDataIds == null)
            {
                Configuration.experimentDataIds = new Dictionary<string, int>();
            }

            //update existing key
            if (Configuration.experimentDataIds.ContainsKey(key))
            {
                var id = Configuration.experimentDataIds[key];
                StartCoroutine(Rest.EnqueuePut("ExperimentData", id, form, debugOutput: false, callback: (succ, result) =>
                {
                    if (!succ)
                    {
                        Debug.LogError("Failed to set experiment data for key: " + key);
                    }
                }));
            }
            else //create new key
            {
                StartCoroutine(Rest.EnqueuePost("ExperimentData", form, debugOutput: false, callback: (succ, result) =>
                {
                    if (succ)
                    {
                        Configuration.experimentDataIds[key] = result["id"];
                    }
                    else
                    {
                        Debug.LogError("Failed to set experiment data for key: " + key);
                    }
                }));
            }
        }

        /// <summary>
        /// Gets all experiment data as a dictionary. Data is obtained from the local cache, which is loaded when game first starts. To ensure data is updated, call FetchExperimentData() to get the latest data from the server first.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetAllExperimentData()
        {
            return Configuration.experimentData;
        }

        public void FetchExperimentData(Rest.ServerCallback callback = null)
        {
            var form = Rest.GetWWWFormWithExperimentInfo();
            Rest.EnqueueGet("ExperimentData", form, (succ, result) =>
            {
                if (succ)
                {
                    foreach (var kvp in result.AsArray.Values)
                    {
                        Configuration.experimentData[kvp["keyName"]] = kvp["value"];
                        Configuration.experimentDataIds[kvp["keyName"]] = kvp["id"];
                    }
                    DebugConfiguration();
                    callback?.Invoke(succ, result);
                }
            });
        }
    }
}