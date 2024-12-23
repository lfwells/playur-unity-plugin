using System.Collections;
using PlayUR.Core;
using PlayUR.DetachedMode;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        public bool IsDetachedMode => Settings.detachedMode;
        public PlayURConfigurationObject DetchedConfiguration => Settings.detachedModeConfiguration;
        public DetachedModeProxyHandler DetachedModeProxy => new();

        public class DetachedModeProxyHandler
        {
            public IEnumerator GetConfiguration(PlayURPlugin plugin)
            {
                var c = new Configuration
                {
                    experiment = plugin.DetchedConfiguration.experiment,
                    experimentID = (int)plugin.DetchedConfiguration.experiment,
                    experimentGroup = plugin.DetchedConfiguration.experimentGroup,
                    experimentGroupID = (int)plugin.DetchedConfiguration.experimentGroup,
                    parameters = new Dictionary<string,string>(plugin.DetchedConfiguration.parameterValues.Select(p => new KeyValuePair<string,string>(p.key, p.value))),
                };
                plugin.configuration = c;
                yield return 0;
            }

            public void GetLeaderboardEntries(string leaderboardID, LeaderboardConfiguration leaderBoardConfiguration, Rest.ServerCallback callback)
            {
                if (callback != null) callback(false);
            }

            public void PlayerPrefsLoad(PlayUR.Core.Rest.ServerCallback callback)
            {
                if (callback != null) callback(false);
            }
            public int PlayerPrefsGetInt(string key, int defaultValue = 0)
            {
                return UnityEngine.PlayerPrefs.GetInt(key, defaultValue);
            }
            public string PlayerPrefsGetString(string key, string defaultValue = "")
            {
                return UnityEngine.PlayerPrefs.GetString(key, defaultValue);
            }
            public float PlayerPrefsGetFloat(string key, float defaultValue = 0)
            {
                return UnityEngine.PlayerPrefs.GetFloat(key, defaultValue);
            }
            public bool PlayerPrefsGetBool(string key, bool defaultValue = false)
            {
                return UnityEngine.PlayerPrefs.GetString(key, defaultValue.ToString()) == "true";
            }
            public bool PlayerPrefsHasKey(string key) 
            {
                return UnityEngine.PlayerPrefs.HasKey(key);
            }
            public void PlayerPrefsDeleteKey(string key)
            {
                UnityEngine.PlayerPrefs.DeleteKey(key);
            }
            public void PlayerPrefsDeleteAll()
            {
                UnityEngine.PlayerPrefs.DeleteAll();
            }
        }
    }
}
