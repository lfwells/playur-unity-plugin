using System.Collections;
using PlayUR.Core;
using PlayUR.DetachedMode;
using System.Linq;
using System.Collections.Generic;
using System;
using UnityEngine;
using PlayUR.Exceptions;
using System.IO;
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
                    analyticsColumnsOrder = Enum.GetValues(typeof(AnalyticsColumn)).Cast<AnalyticsColumn>().ToList(),
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
            public void StandaloneTokenLogin(PlayURPlugin plugin, string stoken, Rest.ServerCallback callback)
            {
                throw new NotImplementedException();
            }
            public void Register(PlayURPlugin plugin, string username, string password, string email, string firstName, string lastName, Rest.ServerCallback callback)
            {
                throw new NotImplementedException();
            }
            #endregion

            #region Analytics
            static StreamWriter currentAnalyticsFile;
            string DetachedModeAnalyticsPath
            {
                get
                {
                    switch (Settings.detachedModeAnalyticsPath)
                    {
                        default:
                        case DetachedModeAnalyticsLocation.PersistentData:
                            return Application.persistentDataPath;

                        case DetachedModeAnalyticsLocation.ExecutableFolder:
                            // parent folder of Application.dataPath
                            return Directory.GetParent(Application.dataPath).FullName;

                        case DetachedModeAnalyticsLocation.GameData:
                            return Application.dataPath;

                        case DetachedModeAnalyticsLocation.Desktop:
                            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                        case DetachedModeAnalyticsLocation.Documents:
                            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }

                }
            }
            List<string> exclude = new List<string>{ "userID", "gameID", "clientSecret", "buildID", "branch" };

            public void StartSession(PlayURPlugin plugin, Dictionary<string, string> form)
            {
                var currentTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var path = Path.Combine(DetachedModeAnalyticsPath, "session_" + currentTimestamp + ".csv");
                currentAnalyticsFile = new StreamWriter(path, true);

                var analyticsHeaders = "action,timestamp,"+ string.Join(",", Enum.GetValues(typeof(AnalyticsColumn)).Cast<AnalyticsColumn>().Select(ac => ac.ToString())) + "\n";
                var toWrite = string.Join("\n", form.Where(kvp => exclude.Contains(kvp.Key) == false).Select(kvp => kvp.Key+","+kvp.Value))+"\n\n"+analyticsHeaders;
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
                    foreach (var action in actions.actions)
                    {
                        var toWrite = action.a.ToString() + "," + action.timestamp + "," + string.Join(",", plugin.Configuration.analyticsColumnsOrder.Select(c => action.columnData.GetValueOrDefault(c, defaultValue: string.Empty)));
                        currentAnalyticsFile.WriteLine(toWrite);
                    }
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

    /// <summary>
    /// Enum used to specify the location of the analytics file in Detached Mode.
    /// </summary>
    public enum DetachedModeAnalyticsLocation
    {
        /// <summary>
        /// Appliation.persistentDataPath
        /// </summary>
        PersistentData,

        /// <summary>
        /// Path one-up from Application.dataPath (should be the location of the EXE/APP file)
        /// </summary>
        ExecutableFolder,

        /// <summary>
        /// Application.dataPath
        /// </summary>
        GameData,


        /// <summary>
        /// Finds the user's desktop path (uses Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        /// </summary>
        Desktop,

        /// <summary>
        /// Finds the user's documents path (uses Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        /// </summary>
        Documents,
    }
}
