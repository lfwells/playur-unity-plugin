using System.Collections;
using PlayUR.Core;
using PlayUR.DetachedMode;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;
using UnityEngine;
using PlayUR.Exceptions;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        public static bool IsDetachedMode => Settings.detachedMode;

        void InitDetachedFunctionality()
        {
            if (IsDetachedMode)
            {
                _detachedConfiguration = Settings.detachedModeConfiguration;
            }
        }
        PlayURConfigurationObject _detachedConfiguration;
        public PlayURConfigurationObject DetchedConfiguration
        {
            get
            {
                return _detachedConfiguration;
            }
            set
            {
                if (inSession)
                    throw new SessionAlreadyStartedException();

                _detachedConfiguration = value;
                StartCoroutine(Init());
            }
        }
        public static DetachedModeProxyHandler DetachedModeProxy => new();

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
                    analyticsColumnsOrder = new List<AnalyticsColumn>(),
                };
                plugin.configuration = c;
                yield return 0;
            }

            #region Login and Register
            public void LoginCanvasAwake(PlayURLoginCanvas loginCanvas)
            {
                var currentTimestampAsUsername = DateTime.Now.ToString("yyyyMMddHHmmss");
                var passwordDoesntMatter = "";

                PlayURPlugin.instance.Login(currentTimestampAsUsername, passwordDoesntMatter, (succ, result) =>
                {
                    PlayURLoginCanvas.LoggedIn = true;
                    loginCanvas.GoToNextScene();
                });
            }
            public void Login(PlayURPlugin plugin, string username, string password, Rest.ServerCallback callback)
            {
                plugin.user = new User
                {
                    name = username,
#if UNITY_EDITOR
                    accessLevel = 9001
#endif
                };
                callback?.Invoke(true, null);
            }
            public void Register(PlayURPlugin plugin, string username, string password, string email, string firstName, string lastName, Rest.ServerCallback callback)
            {
                throw new NotImplementedException();
            }
            #endregion

            #region Analytics
            static StreamWriter currentAnalyticsFile;
            public void StartSession(PlayURPlugin plugin, Dictionary<string, string> form)
            {
                var currentTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var path = Path.Combine(Application.persistentDataPath, "session_" + currentTimestamp + ".csv");
                currentAnalyticsFile = new StreamWriter(path, true);
                var toWrite = string.Join("\n", form.Select(kvp => kvp.Key+","+kvp.Value));
                currentAnalyticsFile.Write(toWrite);
                currentAnalyticsFile.Flush();
                Log("Analytics File Created in Detached Mode at " + path + " with session info " + toWrite);
            }
            public void EndSession(PlayURPlugin plugin, bool startNew = false, Dictionary<string, string> form = null)
            {
                if (currentAnalyticsFile != null) { currentAnalyticsFile.Close(); }
            }
            public IEnumerator StartSessionAsync(PlayURPlugin plugin, Dictionary<string, string> form)
            {
                StartSession(plugin, form);
                yield return 0;
            }
            public IEnumerator EndSessionAsync(PlayURPlugin plugin, bool startNew = false, Dictionary<string, string> form = null)
            {
                EndSession(plugin, startNew, form);
                yield return 0;
            }
            public IEnumerator RecordActionDirectly(PlayURPlugin plugin, ActionParamsList actions, Rest.ServerCallback callback)
            {
                if (currentAnalyticsFile != null)
                {
                    var toWrite = JsonUtility.ToJson(actions);
                    currentAnalyticsFile.WriteLine(toWrite);
                    currentAnalyticsFile.Flush();
                }
                callback?.Invoke(true, null);
                yield return 0;
            }
            public IEnumerator ProcessUpdatableAction<T>(UpdatableAction<T> updatableAction) 
            { 
                throw new NotImplementedException();
                yield break;
            }
            public IEnumerator BackupSessionAsync(PlayURPlugin plugin, Dictionary<string, string> form)
            {
                throw new NotImplementedException();
                yield break;
            }
            #endregion

            #region Leaderboard
            public void GetLeaderboardEntries(PlayURPlugin plugin, string leaderboardID, LeaderboardConfiguration leaderBoardConfiguration, Rest.ServerCallback callback)
            {
                callback?.Invoke(false, null);
            }
            public void UpdateLeaderboardEntryName(PlayURPlugin plugin, int id, string name, Rest.ServerCallback callback)
            {
                callback?.Invoke(false, null);
            }
            #endregion

            #region MTurk and Prolific
            public IEnumerator InitMTurk(PlayURPlugin plugin)
            {
                throw new NotImplementedException();
                yield break;
            }
            public IEnumerator InitProlific(PlayURPlugin plugin)
            {
                throw new NotImplementedException();
                yield break;
            }
            public void MarkMTurkComplete(PlayURPlugin plugin)
            {
                throw new NotImplementedException();
            }
            public void MarkProlificComplete(PlayURPlugin plugin)
            {
                throw new NotImplementedException();
            }
            #endregion

            #region PlayerPrefs
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
            #endregion
        }
    }
}
