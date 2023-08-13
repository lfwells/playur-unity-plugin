using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using PlayUR.Exceptions;
using PlayUR.Core;
using PlayUR;

namespace PlayUR
{
    #region Sub-Classes
    /// <summary>
    /// Represents a user on the Platform
    /// </summary>
    public class User
    {
        /// <summary>
        /// The user's ID
        /// </summary>
        public int id;

        /// <summary>
        /// The user's username
        /// </summary>
        public string name;

        /// <summary>
        /// Is the user listed as an owner of this game?
        /// </summary>
        public bool IsGameOwner { get { return accessLevel > NO_ACCESS; } }

        /// <summary>
        /// The user's defined access level as defined on Owners tab of the platform. Will be -1 if not an owner
        /// </summary>
        public int accessLevel = NO_ACCESS;
        const int NO_ACCESS = -1;
    }
    /// <summary>
    /// Represents the settings for an inidivdual user playing the game.
    /// Calculated as a combination of global \ref elements and \ref parameters which can be overridden
    /// at the Experiment and ExperimentGroup level.
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// The ID of the current experiment being run. 
        /// </summary>
        public int experimentID;

        /// <summary>
        /// The current experiment being run, in enum form. 
        /// </summary>
        public Experiment experiment;

        /// <summary>
        /// The ID of the current experiment group this user has been allocated to. 
        /// </summary>

        public int experimentGroupID;
        /// <summary>
        /// The current experiment group this user has been allocated to, in enum form. 
        /// </summary>
        public ExperimentGroup experimentGroup;

        /// <summary>
        /// List of active Game Elements for this current configuration. If an element is not in this list, it is not active. 
        /// </summary>
        public List<Element> elements;

        /// <summary>
        /// Key-Value-Pairs of the enabled Parameters for this current configuration. 
        ///May be used for modifying UI text, or configuring enemy counts, etc etc.
        /// </summary>
        public Dictionary<string, string> parameters;

        /// <summary>
        /// The list of extra analytics columns, but sorted by custom sort order from admin page.
        /// </summary>
        public List<AnalyticsColumn> analyticsColumnsOrder;

        /// <summary>
        /// The build ID of the current configuration
        /// </summary>
        public int buildID;

        /// <summary>
        /// The branch of the current build
        /// </summary>
        public string branch;
    }
    #endregion

    /// <summary>
    /// Container singleton class containing state information and functions for interacting with the platform.
    /// An instance of this class exists within the PlayURLogin scene. It is auto-generated and you shouldn't need to modify it.
    /// </summary>
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        #region Configuration and Set Up
        /// <summary>Matches the id field of the relevant game in the Game table in the server database.
        /// Is set on initial "Set Up" process, however if you need to update it, can be updated by running the set up process again.
        /// </summary>
        public int gameID;

        /// <summary>Matches the client_secret field of the relevant game in the Game table in the server database.
        /// Is set on initial "Set Up" process, however if you need to update it, can be updated by running the set up process again.
        /// </summary>
        public string clientSecret;

        /// <summary> The currently logged in user. Will be null before log in. </summary>
        public User user;


        /// <summary>The base url of the server instance through which all Rest requests will go. 
        /// Should point to the "api" sub-directory on the server.
        /// </summary>
        public const string SERVER_URL = "https://playur.io/api/";

        /// <summary>The prefix to use for all stored client-side PlayerPrefs information. 
        /// Used to avoid clashes with user-defined PlayerPrefs.
        /// </summary>
        public const string PERSIST_KEY_PREFIX = "PlayUR_";

        /// <summary>
        /// Stores browser name and version number. Only valid for WebGL builds. See <see cref="PlayURLoginCanvas.WebGLLogin(string)"/> and <seealso cref="StartSession" />.
        /// </summary>
        public static string browserInfo;

        /// <summary>Used to determine if the plugin is ready for normal use (i.e. the user has logged in, and a configuration has been obtained.</summary>
        /// <value><c>true</c> if the user has logged in, and the configuration has been set.
        /// <c>false</c> if the user has not logged in yet, or a configuration could not be found.</value>
        public bool IsReady => inited;

        [Obsolete("Use PlayURPlugin.instance.IsReady instead.")]
        /// <summary>Used to determine if the plugin is ready for normal use (i.e. the user has logged in, and a configuration has been obtained.</summary>
        /// <value><c>true</c> if the user has logged in, and the configuration has been set.
        /// <c>false</c> if the user has not logged in yet, or a configuration could not be found.</value>
        public static bool Ready { get { return instance.inited; } }

        /// <summary>
        /// The current configuration of the plugin. Contains the current experiment, experiment group, and active elements and parameters.
        /// However it is recommended to use the <see cref="CurrentExperiment"/>, <see cref="CurrentExperimentGroup"/>, <see cref="CurrentElements"/> and <see cref="CurrentParameters"/> properties instead.
        /// </summary>
        public Configuration Configuration { get { return configuration; } }

        protected Configuration configuration;
        bool inited;

        /// <summary>Basic Unity Singleton pattern. Triggers the initialization process. Also checks to see if the gameID and clientSecret is set. 
        /// If gameID is not set, quits the application. </summary>
        public override void Awake()
        {
            if (exists && instance != this) { DestroyImmediate(this); return; }

            if (gameID <= 0)
            {
                Debug.LogError("Game ID must be > 0");
                Debug.Break();
                Quit();
                return;
            }
            if (string.IsNullOrEmpty(clientSecret))
            {
                Debug.LogError("Client Secret ID must be set");
                Debug.Break();
                Quit();
                return;
            }

            base.Awake();

            StartCoroutine(Init());
        }

        /// <summary>Logs in the user, and gets the configuration for that user. Loads scene #1 after this process is complete.</summary>
        public IEnumerator Init()
        {
            // Start the queue
            StartCoroutine(Rest.Queue.StartProcessing());

            //login (if required...)
            while (PlayURLoginCanvas.LoggedIn == false)
            {
                yield return GetLogin();
            }

            //get game config
            yield return GetConfiguration();
            if (experimentFull)
            {
                if (PlayURLoginCanvas.exists) PlayURLoginCanvas.instance.CancelLogin();
                SceneManager.LoadScene("PlayURLogin");
                yield return new WaitForEndOfFrame();
                PlayURLoginCanvas.instance.ShowError("Experiment has closed, please check with game owner for more details.");
                throw new ExperimentGroupsFullException(user, gameID);
            }

            else if (configuration == null)
            {
                StartCoroutine(Init());
                if (PlayURLoginCanvas.exists) PlayURLoginCanvas.instance.CancelLogin();

                throw new GameNotOwnedException(user, gameID);
            }
            else
            {
                //set up mTurk integration as needed
                StartCoroutine(InitMTurk());

                inited = true;

                DebugConfiguration();
            }

            //return to this scene
            //SceneManager.LoadScene(1);

            OnReady.Invoke();

            //set up the action batcher
            StartCoroutine(RecordActionRoutine());
        }

        /// <summary>Loads the login scene. Waits until the PlayURLoginCanvas obtains a valid login. </summary>
        public static IEnumerator GetLogin()
        {
            if (PlayURLoginCanvas.LoggedIn)
                yield break;

            //open the login scene
            SceneManager.LoadScene("PlayURLogin");

            //wait for login clicked
            yield return new WaitForEndOfFrame();
            while (PlayURLoginCanvas.LoggedIn == false)
            {
                yield return new WaitForEndOfFrame();
            }

            yield return 0;
        }

        /// <summary>Performs a log in request to the server.
        /// After the callback the user will be populated (if correct username and password).</summary>
        /// <param name="username">the username (not id) of the user. Entered in the PlayURLoginCanvas, or may be set by the WebGL integration.</param>
        /// <param name="password">the password (or token) of the user. Entered in the PlayURLoginCanvas, or may be set by the WebGL integration.</param>
        /// <param name="callback">called when the response from the server is given, with a bool success and the user's information</param>
        public void Login(string username, string password, Rest.ServerCallback callback)
        {
            var form = Rest.GetWWWForm();
            form.Add("username", username);
            form.Add("password", password);

            StartCoroutine(Rest.EnqueuePost("Login", form, (succ, result) =>
            {
                if (succ)
                {
                    user = new User();
                    user.name = username;
                    user.id = result["id"];

                    PlayerPrefs.Load(callback);
                    StartCoroutine(PlayerPrefs.PeriodicallySavePlayerPrefs());
                }
                callback(succ, result);
            }, debugOutput: false));
        }

        /// <summary>Performs a register request to the server.
        /// After the callback the user will be populated (if correct username and password).</summary>
        /// <param name="username">the username of the new  user. Entered in the PlayURLoginCanvas.</param>
        /// <param name="password">the password of the new  user. Entered in the PlayURLoginCanvas.</param>
        /// <param name="email">the email of the new user. Entered in the PlayURLoginCanvas.</param>
        /// <param name="firstName">the first name of the  new user. Entered in the PlayURLoginCanvas. Optional, leave as empty string to skip.</param>
        /// <param name="lastName">the last name of the  new user. Entered in the PlayURLoginCanvas. Optional, leave as empty string to skip.</param>
        /// <param name="callback">called when the response from the server is given, with a bool success and the user's information</param>
        public void Register(string username, string password, string email, string firstName, string lastName, Rest.ServerCallback callback)
        {
            var form = Rest.GetWWWForm();
            form.Add("username", username);
            form.Add("email", email);
            form.Add("fname", firstName);
            form.Add("lname", lastName);
            form.Add("password", password);
            form.Add("confirm", password);

            StartCoroutine(Rest.EnqueuePost("Register", form, (succ, result) =>
            {
                if (succ)
                {
                    user = new User();
                    user.name = username;
                    user.id = result["id"];

                    PlayerPrefs.Load(callback);
                }
                callback(succ, result);
            }, debugOutput: false));
        }


        [System.NonSerialized] public bool didRequestExperiment = false;
        [System.NonSerialized] public bool didRequestExperimentGroup = false;
        [System.NonSerialized] public Experiment requestedExperiment;
        [System.NonSerialized] public ExperimentGroup requestedExperimentGroup;

        bool experimentFull = false;

        /// <summary>loads the config for the given user</summary>
        IEnumerator GetConfiguration()//(Action action, ServerResponse callback, params object[] p)
        {
            Log("Getting Configuration...");
            var form = Rest.GetWWWForm();

            experimentFull = false;

            bool experimentOverrideFound = false;
            Experiment? experiment = null;
            //try and get an experiment from the experiment URL
            if (didRequestExperiment)
            {
                experimentOverrideFound = true;
                experiment = requestedExperiment;
            }
#if (UNITY_ANDROID || UNITY_IOS)  && !UNITY_EDITOR
            if (experimentOverrideFound == false && PlayURPluginHelper.instance != null)
            {
                experimentOverrideFound = PlayURPluginHelper.instance.useSpecificExperimentForMobileBuild;
                experiment = PlayURPluginHelper.instance.mobileExperiment;
            }
#elif UNITY_STANDALONE && !UNITY_EDITOR
            if (experimentOverrideFound == false && PlayURPluginHelper.instance != null)
            {
                experimentOverrideFound = PlayURPluginHelper.instance.useSpecificExperimentForDesktopBuild;
                experiment = PlayURPluginHelper.instance.desktopExperiment;
            }
#endif
            //if not found, try and get an experiment from the PluginHelper script
            if (Application.isEditor && experimentOverrideFound == false && PlayURPluginHelper.instance != null)
            {
                experimentOverrideFound = PlayURPluginHelper.instance.forceToUseSpecificExperiment;
                experiment = PlayURPluginHelper.instance.experimentToTestInEditor;
            }
            if (experimentOverrideFound && experiment.HasValue)
            {
                form.Add("experimentID", ((int)experiment.Value).ToString());
                Log("Using Experiment Override " + experiment.Value.ToString());
            }

            bool experimentGroupOverrideFound = false;
            ExperimentGroup? experimentGroup = null;

            if (didRequestExperimentGroup)
            {
                experimentGroupOverrideFound = true;
                experimentGroup = requestedExperimentGroup;
            }
            //if not found, try and get an experiment group from the PluginHelper script
            if (Application.isEditor && experimentGroupOverrideFound == false && PlayURPluginHelper.instance != null)
            {
                experimentGroupOverrideFound = PlayURPluginHelper.instance.forceToUseSpecificGroup;
                experimentGroup = PlayURPluginHelper.instance.groupToTestInEditor;
            }
            if (experimentGroupOverrideFound && experimentGroup.HasValue)
            {
                form.Add("experimentGroupID", ((int)experimentGroup.Value).ToString());
                Log("Using Experiment Group Override " + experimentGroup.Value.ToString());
            }

            //go ahead and get config now
            yield return StartCoroutine(Rest.EnqueueGet("Configuration", form, (succ, result) =>
            {
                if (succ)
                {
                    configuration = new Configuration();
                    /* format:
                     * elements [string array]
                     * parameters [object array; fields are key,value]
                    */
                    var elements = result["elements"];

                    configuration.experimentID = result["experiment"]["id"];
                    configuration.experimentGroupID = result["group"]["id"];

                    configuration.experiment = (Experiment)configuration.experimentID;
                    configuration.experimentGroup = (ExperimentGroup)configuration.experimentGroupID;

                    configuration.branch = result["branch"];
                    configuration.buildID = result["buildID"];

                    configuration.elements = new List<Element>();
                    foreach (var element in elements)
                    {
                        configuration.elements.Add((Element)element.Value["id"].AsInt);
                    }

                    var parameters = result["parameters"];
                    configuration.parameters = new Dictionary<string, string>();
                    foreach (var p in parameters)
                    {
                        configuration.parameters.Add(p.Key, p.Value);
                    }

                    configuration.analyticsColumnsOrder = new List<AnalyticsColumn>();
                    var inColumns = new List<JSONNode>();
                    foreach (var column in result["analyticsColumns"])
                    {
                        inColumns.Add(column.Value);
                    }
                    inColumns.Sort((a, b) =>
                    {
                        if (a["sort"].AsInt == b["sort"].AsInt)
                        {
                            return a["id"].AsInt.CompareTo(b["id"].AsInt);
                        }
                        return a["sort"].AsInt.CompareTo(b["sort"].AsInt);
                    });
                    foreach (var column in inColumns)
                    {
                        var columnAsEnum = (AnalyticsColumn)(column["id"].AsInt);
                        configuration.analyticsColumnsOrder.Add(columnAsEnum);
                    }

                    if (result.HasKey("accessLevel"))
                        user.accessLevel = result["accessLevel"].AsInt;
                }
                else
                {
                    if (result != null && result.HasKey("closed") && result["closed"].AsBool == true)
                    {
                        LogError("No experiment group left with enough spots to allocate user! Check max members setting on experiment group. This error can also occur if you don't have any Experiment Groups configured for an Experiment.", breakCode: true);
                        experimentFull = true;
                    }
                    configuration = null;
                }
            }, debugOutput: false));
        }
        /// <summary>used for debugging to the Unity console the elements included and the key value pair parameters.</summary>
        void DebugConfiguration()
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }

            var s =
                $"CONFIGURATION:" +
                $"\n\tExperimentID:{configuration.experimentID} - {configuration.experiment}" +
                $"\n\tExperimentGroupID: {configuration.experimentGroupID} - {configuration.experimentGroup}" +
                $"\n\tBuildID: {configuration.buildID}, Branch: {configuration.branch}" +
                $"\n\t" +
                $"\n\tElements:\n";
            foreach (var element in configuration.elements)
            {
                s += "\t\t" + element + "\n";
            }

            s += "\tParameters:\n";
            foreach (var p in configuration.parameters)
            {
                s += "\t\t" + p.Key + "\t" + p.Value + "\n";
            }

            s += "\tUser:\n\t\t" + user.name + "\n\t\tID = " + user.id + "\n\t\tAccess Level = " + user.accessLevel + "\n";

            s += "\tAnalytics columns:\n";
            foreach (var c in configuration.analyticsColumnsOrder)
            {
                s += "\t\t" + c + "\n";
            }

            Log(s);
        }

        /// <summary>Event called when <see cref="IsReady"/> becomes <c>true</c>. If a new listener is added when the plugin is already ready, the code will be executed immediately.</summary>
        public PlayURReadyEvent OnReady = new PlayURReadyEvent();

        #endregion

        #region Game Element Getters
        /// <summary>Gets all enabled Game Elements from the current configuration.</summary>
        /// <returns>a list of the active Game Elements.</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public List<Element> ListElements()
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            return configuration.elements;
        }

        /// <summary>Query if a certain element is enabled or not</summary>
        /// <returns>true if the given element is enabled.</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public bool ElementEnabled(Element element)
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            return configuration.elements.Contains(element);
        }

        /// <summary>Currently running experiment.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public Experiment CurrentExperiment
        {
            get
            {
                if (configuration == null)
                {
                    throw new ConfigurationNotReadyException();
                }
                return configuration.experiment;
            }
        }

        /// <summary>Currently running experiment group.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public ExperimentGroup CurrentExperimentGroup
        {
            get
            {
                if (configuration == null)
                {
                    throw new ConfigurationNotReadyException();
                }
                return configuration.experimentGroup;
            }
        }

        /// <summary>Current build number of this experiment.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public int CurrentBuildID
        {
            get
            {
                if (configuration == null)
                {
                    throw new ConfigurationNotReadyException();
                }
                return configuration.buildID;
            }
        }

        /// <summary>Current build branch of this build.</summary>
        /// <exception cref="ConfigurationNotReadyException">thrown if configuration is not previously obtained</exception>
        public string CurrentBuildBranch
        {
            get
            {
                if (configuration == null)
                {
                    throw new ConfigurationNotReadyException();
                }
                return configuration.branch;
            }
        }

        #endregion

        #region Parameter Getters
        /// <summary>
        /// Return a list of all parameters defined in the <see cref="Configuration"/>.
        /// </summary>
        /// <returns>The parameters in Dictionary format, keys are parameter names, values are the associated values.</returns>
        /// <exception cref="ConfigurationNotReadyException"></exception>
        public Dictionary<string, string> ListParameters()
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            return configuration.parameters;
        }

        /// <summary>
        /// Check if a given parameter key exists in the <see cref="Configuration"/>.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        /// <exception cref="ConfigurationNotReadyException"></exception>
        public bool ParamExists(string key)
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            return configuration.parameters.ContainsKey(key);
        }

        /// <summary>
        /// Obtains a value of a parameter defined in the <see cref="Configuration"/>. This is the base-level function intended to be internal.
        /// All parameters are initially obtained as strings and must be converted to their type.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        string GetParam(string key)
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            string result = null;
            if (configuration.parameters.TryGetValue(key, out result))
            {
                return result;
            }
            throw new ParameterNotFoundException(key);
        }

        /// <summary>
        /// Obtains a value of a parameter defined in the <see cref="Configuration"/>. This is the base-level function intended to be internal.
        /// All parameters are initially obtained as strings and must be converted to their type.
        /// This version includes a default value to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        string GetParam(string key, string defaultValue = "", bool warn = true)
        {
            if (configuration == null)
            {
                throw new ConfigurationNotReadyException();
            }
            string result;
            if (configuration.parameters.TryGetValue(key, out result) == false)
            {
                result = defaultValue;
                if (warn) LogWarning(string.Format("Tried to get value for {0} but was not set. Defaulting to {1}", key, defaultValue));
            }
            return result;
        }

        /// <summary>
        /// Obtains a string value of a parameter defined in the <see cref="Configuration"/> in string form.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The string value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        public string GetStringParam(string key)
        {
            return GetParam(key);
        }


        /// <summary>
        /// Obtains a string value of a parameter defined in the <see cref="Configuration"/> in string form.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The string value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        public string GetStringParam(string key, string defaultValue = "", bool warn = true)
        {
            return GetParam(key, defaultValue, warn);
        }

        /// <summary>
        /// Obtains the value of a parameter defined in the <see cref="Configuration"/> in integer form.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The integer value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to an integer</exception>
        public int GetIntParam(string key)
        {
            int result = 0;
            if (int.TryParse(GetParam(key), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(int));
        }

        /// <summary>
        /// Obtains the value of a parameter defined in the <see cref="Configuration"/> in integer form.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The integer value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to an integer</exception>
        public int GetIntParam(string key, int defaultValue = -1, bool warn = true)
        {
            int result = 0;
            if (int.TryParse(GetParam(key, defaultValue.ToString(), warn), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(int));
        }

        /// <summary>
        /// Obtains the value of a parameter defined in the <see cref="Configuration"/> in float form.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The float value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to a float</exception>
        public float GetFloatParam(string key)
        {
            float result = 0;
            if (float.TryParse(GetParam(key), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(float));
        }

        /// <summary>
        /// Obtains the value of a parameter defined in the <see cref="Configuration"/> in float form.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The float value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to a float</exception>
        public float GetFloatParam(string key, float defaultValue = 0, bool warn = true)
        {
            float result = 0;
            if (float.TryParse(GetParam(key, defaultValue.ToString(), warn), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(float));
        }

        /// <summary>
        /// Obtains an integer value of a parameter defined in the <see cref="Configuration"/> in string form.
        /// Uses whatever logic <see cref="bool.TryParse(string, out bool)" /> uses to convert to bool.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The boolean value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to a boolean</exception>
        public bool GetBoolParam(string key)
        {
            bool result = false;
            if (bool.TryParse(GetParam(key), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(bool));
        }


        /// <summary>
        /// Obtains an integer value of a parameter defined in the <see cref="Configuration"/> in string form.
        /// Uses whatever logic <see cref="bool.TryParse(string, out bool)" /> uses to convert to bool.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The boolean value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if the parameter was unable to be converted to a boolean</exception>
        public bool GetBoolParam(string key, bool defaultValue = false, bool warn = true)
        {
            bool result = false;
            if (bool.TryParse(GetParam(key, defaultValue.ToString(), warn), out result))
            {
                return result;
            }
            throw new InvalidParamFormatException(key, typeof(bool));
        }

        //now all of these again but array versions
        char[] PARAM_LIST_SPLIT_DELIMITER = new char[] { '|', '|', '|' };
        string PARAM_LIST_KEY_APPEND = "[]";

        /// <summary>
        /// Obtains a string array of values of a parameter defined in the <see cref="Configuration"/>.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The string array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        public string[] GetStringParamList(string key)
        {
            string unsplit = GetStringParam(key + PARAM_LIST_KEY_APPEND);
            return unsplit.Split(PARAM_LIST_SPLIT_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Obtains a string array of values of a parameter defined in the <see cref="Configuration"/>.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The string array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        public string[] GetStringParamList(string key, string[] defaultValue, bool warn = true)
        {
            try
            {
                return GetStringParamList(key + PARAM_LIST_KEY_APPEND);
            }
            catch (ParameterNotFoundException)
            {
                if (warn) Debug.LogWarning("Parameter " + key + " not found, using default value");
                return defaultValue;
            }
        }

        /// <summary>
        /// Obtains a int array of values of a parameter defined in the <see cref="Configuration"/>.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The int array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if one or more values in the array were unable to be converted to an integer</exception>
        public int[] GetIntParamList(string key)
        {
            string unsplit = GetStringParam(key + PARAM_LIST_KEY_APPEND);
            string[] split = unsplit.Split(PARAM_LIST_SPLIT_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
            int[] result = new int[split.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = int.Parse(split[i]);
            }
            return result;
        }

        /// <summary>
        /// Obtains a int array of values of a parameter defined in the <see cref="Configuration"/>.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The int array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if one or more values in the array were unable to be converted to an integer</exception>
        public int[] GetIntParamList(string key, int[] defaultValue, bool warn = true)
        {
            try
            {
                return GetIntParamList(key + PARAM_LIST_KEY_APPEND);
            }
            catch (ParameterNotFoundException)
            {
                if (warn) Debug.LogWarning("Parameter " + key + " not found, using default value");
                return defaultValue;
            }
            catch (ArgumentNullException) //thrown if int.Parse fails
            {
                throw new InvalidParamFormatException(key, typeof(int));
            }
        }

        /// <summary>
        /// Obtains a boolean array of values of a parameter defined in the <see cref="Configuration"/>.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The boolean array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if one or more values in the array were unable to be converted to a boolean</exception>
        public bool[] GetBoolParamList(string key)
        {
            string unsplit = GetStringParam(key + PARAM_LIST_KEY_APPEND);
            string[] split = unsplit.Split(PARAM_LIST_SPLIT_DELIMITER, StringSplitOptions.RemoveEmptyEntries);
            bool[] result = new bool[split.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = bool.Parse(split[i]);
            }
            return result;
        }

        /// <summary>
        /// Obtains a boolean array of values of a parameter defined in the <see cref="Configuration"/>.
        /// This version includes a defaultValue to return if the parameter is not found.
        /// </summary>
        /// <param name="key">The key matching the parameter name set on the back-end</param>
        /// <returns>The boolean array of value of the requested parameter if it exists</returns>
        /// <exception cref="ConfigurationNotReadyException">thrown if <see cref="Configuration"/> is not previously obtained</exception>
        /// <exception cref="ParameterNotFoundException">thrown if no parameter with that name present in the <see cref="Configuration"/></exception>
        /// <exception cref="InvalidParamFormatException">thrown if one or more values in the array were unable to be converted to a boolean</exception>
        public bool[] GetBoolParamList(string key, bool[] defaultValue, bool warn = true)
        {
            try
            {
                return GetBoolParamList(key + PARAM_LIST_KEY_APPEND);
            }
            catch (ParameterNotFoundException)
            {
                if (warn) Debug.LogWarning("Parameter " + key + " not found, using default value");
                return defaultValue;
            }
            catch (ArgumentNullException) //thrown if int.Parse fails
            {
                throw new InvalidParamFormatException(key, typeof(bool));
            }
        }

        #endregion

        #region Session Logging
        bool inSession;
        const int NO_SESSION = -1;
        int sessionID = NO_SESSION;
        public int CurrentSession
        {
            get { return sessionID; }
        }
        public bool CurrentSessionRunning
        {
            get { return inSession; }
        }

        /// <summary>
        /// Starts logging a new session (if not already in a session). Records system information, note that on WebGL, returned information
        /// may be inconsistent or incorrect, based upon browser compatability--use with caution.
        /// </summary>
        /// <exception cref="SessionAlreadyStartedException">thrown if there is already a running session.</exception>
        public void StartSession()
        {
            if (inSession) return;
            //                throw new SessionAlreadyStartedException(); //meh
            Log("Session Started");

            var form = Rest.GetWWWFormWithExperimentInfo();
            form.Add("start", GetMysqlFormatTime());
            //form.Add("experimentID", configuration.experimentID.ToString());

            form.Add("buildID", CurrentBuildID.ToString());
            form.Add("branch", CurrentBuildBranch);

            form.Add("operatingSystem", SystemInfo.operatingSystem);
            form.Add("deviceType", SystemInfo.deviceType.ToString());
            form.Add("deviceModel", SystemInfo.deviceModel);
            //deviceName is probably too personal
            form.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier); //presumably mac address
            form.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
            form.Add("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
            form.Add("processorType", SystemInfo.processorType);
            form.Add("currentResolution", Screen.currentResolution.ToString());
            if (string.IsNullOrEmpty(browserInfo) == false) { form.Add("browserInfo", browserInfo); }

            StartCoroutine(Rest.EnqueuePost("Session", form, (succ, result) =>
            {
                if (succ)
                {
                    sessionID = result["id"];
                    inSession = true;

                    // Start automated backups
                    _periodicBackup = StartCoroutine(PeriodicallyBackupSession());
                }
            }, debugOutput: false));
        }
        /// <summary>
        /// Starts logging a new session (if not already in a session). Records system information, note that on WebGL, returned information
        /// may be inconsistent or incorrect, based upon browser compatability--use with caution.
        /// </summary>
        /// <exception cref="SessionAlreadyStartedException">thrown if there is already a running session.</exception>
        public IEnumerator StartSessionAsync() //TODO fix these async ones to not repeat code
        {
            if (inSession) yield break;
            //                throw new SessionAlreadyStartedException(); //meh

            var form = Rest.GetWWWFormWithExperimentInfo();
            form.Add("start", GetMysqlFormatTime());
            //form.Add("experimentID", configuration.experimentID.ToString());

            form.Add("operatingSystem", SystemInfo.operatingSystem);
            form.Add("deviceType", SystemInfo.deviceType.ToString());
            form.Add("deviceModel", SystemInfo.deviceModel);
            //deviceName is probably too personal
            form.Add("deviceUniqueIdentifier", SystemInfo.deviceUniqueIdentifier); //presumably mac address
            form.Add("graphicsDeviceName", SystemInfo.graphicsDeviceName);
            form.Add("graphicsDeviceType", SystemInfo.graphicsDeviceType.ToString());
            form.Add("processorType", SystemInfo.processorType);
            form.Add("currentResolution", Screen.currentResolution.ToString());
            if (string.IsNullOrEmpty(browserInfo) == false) { form.Add("browserInfo", browserInfo); }

            yield return StartCoroutine(Rest.EnqueuePost("Session", form, (succ, result) =>
            {
                if (succ)
                {
                    sessionID = result["id"];
                    inSession = true;

                    Log("Session Started " + sessionID);
                }
            }, debugOutput: true));
        }

        /// <summary>
        /// Records the end of a session.
        /// </summary>
        /// <param name="startNew">optionally immediately starts a new session after ending this one.</param>
        /// <exception cref="SessionNotStartedException">thrown if there is no session to end.</exception>
        public void EndSession(bool startNew = false)
        {
            if (!inSession || sessionID == -1)
                throw new SessionNotStartedException();

            var endConfig = new Dictionary<string, string>() {
                { "end", GetMysqlFormatTime() },
                { "history", GetHistoryString() },
                { "debugLog", GetDebugLogs(PlayURPluginHelper.instance.minimumLogLevelToStore) }
            };

            StartCoroutine(Rest.EnqueuePut("Session", sessionID, endConfig, (succ, result) =>
            {
                if (succ)
                {
                    sessionID = -1;
                    inSession = false;
                    StopCoroutine(_periodicBackup);

                    if (startNew)
                        StartSession();
                }
            }, debugOutput: false, storeFormInHistory: false));
        }


        /// <summary>
        /// Records the end of a session.
        /// </summary>
        /// <param name="startNew">optionally immediately starts a new session after ending this one.</param>
        /// <exception cref="SessionNotStartedException">thrown if there is no session to end.</exception>
        public IEnumerator EndSessionAsync(bool startNew = false)
        {
            if (!inSession || sessionID == -1)
                throw new SessionNotStartedException();

            var endConfig = new Dictionary<string, string>() {
                { "end", GetMysqlFormatTime() },
                { "history", GetHistoryString() },
                { "debugLog", GetDebugLogs(PlayURPluginHelper.instance.minimumLogLevelToStore) }
            };

            yield return StartCoroutine(Rest.EnqueuePut("Session", sessionID, endConfig, (succ, result) =>
            {
                if (succ)
                {
                    sessionID = -1;
                    inSession = false;
                    StopCoroutine(_periodicBackup);

                    if (startNew)
                        StartSession();
                }
            }, debugOutput: false, storeFormInHistory: false));
        }

        /// <summary>
        /// Records the end of the current session and starts a new one.
        /// Is shorthand for <c>EndSession(startNew: true);</c>
        /// </summary>
        public void EndSessionAndStartANewOne()
        {
            EndSession(startNew: true);
        }


        /// <summary>Backs up the current session</summary>
        public IEnumerator BackupSessionAsync()
        {
            if (!inSession || sessionID == -1)
                throw new SessionNotStartedException();

            var form = new Dictionary<string, string>() {
                { "history", GetHistoryString() },
            };

            return Rest.EnqueuePut("Session", sessionID, form, (succ, result) =>
            {
                PlayURPlugin.Log("session backedup");
            }, debugOutput: false, storeFormInHistory: false);
        }

        private Coroutine _periodicBackup;
        public IEnumerator PeriodicallyBackupSession()
        {
            yield return new WaitForSecondsRealtime(60);
            while (inSession && sessionID != -1)
            {
                yield return BackupSessionAsync();
                yield return new WaitForSecondsRealtime(60);
            }
        }
        #endregion

        #region Leaderboards
        [System.Serializable]
        public class LeaderboardConfiguration
        {
            public string title = "High Scores";

            public bool localOnly = false; //only show our scores + fake data

            [Header("Sort/Filter Options")]
            public bool descending = true;
            public bool oneEntryPerPlayer = false;
            public bool sameExperimentOnly = true;
            public bool sameExperimentGroupOnly = true;

            [Header("Display Options")]
            public int maxItems = int.MaxValue;
            public bool autoScrollToHighlightedRow = true;
            [Tooltip("Leave Blank For Default")]
            public string displayFormat = ""; //element 0 is the number
                                              //if using total seconds bool, 0 = hours, 1 = minutes, 2 = seconds
            [System.Serializable]
            public enum DataType
            {
                Integer,
                Float,
                TimeSeconds,
                TimeMilliseconds,
            }
            public DataType dataType;

            [Header("Name Display Options")]
            public NameDisplayType nameDisplayType;
            public string customNameDefaultValue = "";
            public string anonymousCustomNameValue = "Anonymous";
            public bool closeOnNameEntryComplete = true;

            [System.Serializable]
            public enum NameDisplayType
            {
                FirstName,
                Username,
                CustomName,
            }

            [HideInInspector] public bool onlyShowIfNewEntryAdded = false;
            public Dictionary<string, string> AddToForm(Dictionary<string, string> form)
            {
                form.Add("descending", descending.ToString());
                form.Add("oneEntryPerPlayer", oneEntryPerPlayer.ToString());
                form.Add("maxItems", maxItems.ToString());
                form.Add("sameExperimentOnly", sameExperimentOnly.ToString());
                form.Add("sameExperimentGroupOnly", sameExperimentGroupOnly.ToString());
                form.Add("localOnly", localOnly.ToString());
                return form;
            }
        }
        public void AddLeaderboardEntry(string leaderboardID, float score, LeaderboardConfiguration leaderBoardConfiguration, Rest.ServerCallback callback = null, params object[] extraFields)
        {
            if (string.IsNullOrEmpty(leaderboardID))
            {
                throw new PlayUR.Exceptions.InvalidLeaderboardIDException(leaderboardID);
            }

            var form = Rest.GetWWWForm();
            form.Add("leaderboardID", leaderboardID);
            form.Add("experimentID", configuration.experimentID.ToString());
            form.Add("experimentGroupID", configuration.experimentGroupID.ToString());
            form.Add("score", score.ToString());
            form.Add("extra", string.Join(",", extraFields));
            form = leaderBoardConfiguration.AddToForm(form);

            StartCoroutine(Rest.EnqueuePost("LeaderboardEntry", form, callback: callback, debugOutput: false));
        }
        public void GetLeaderboardEntries(string leaderboardID, LeaderboardConfiguration leaderBoardConfiguration, Rest.ServerCallback callback)
        {
            if (string.IsNullOrEmpty(leaderboardID))
            {
                throw new PlayUR.Exceptions.InvalidLeaderboardIDException(leaderboardID);
            }

            var form = Rest.GetWWWForm();
            form.Add("leaderboardID", leaderboardID);
            form.Add("experimentID", configuration.experimentID.ToString());
            form.Add("experimentGroupID", configuration.experimentGroupID.ToString());
            form = leaderBoardConfiguration.AddToForm(form);

            StartCoroutine(Rest.EnqueueGet("LeaderboardEntry/list.php", form, callback: callback, debugOutput: false));
        }
        public void AddLeaderboardEntryAndShowHighScoreTable(
            string leaderboardID, float score,
            LeaderboardConfiguration configuration,
            GameObject leaderboardPrefab = null,
            Canvas onCanvas = null,
            float height = -1,
            bool showCloseButton = true,
            KeyCode keyCodeForClose = KeyCode.None,
            HighScoreTable.CloseCallback closeCallback = null,
            params object[] extraFields)
        {
            AddLeaderboardEntry(leaderboardID, score, configuration, callback: (succ, data) =>
            {
                //actually for single-entry only entries, succ may be false, and thats fine, should still show highscores
                if (succ || !configuration.onlyShowIfNewEntryAdded)
                {
                    int highlightRowID = -1;
                    if (data != null) int.TryParse(data["id"], out highlightRowID);
                    //Debug.Log("highlight row "+highlightRowID+ "("+data["id"].ToString()+")");
                    ShowHighScoreTable(leaderboardID, configuration, highlightRowID, leaderboardPrefab, onCanvas, height, showCloseButton, keyCodeForClose, closeCallback);
                }
            }, extraFields);
        }
        public void UpdateLeaderboardEntryName(int id, string name, Rest.ServerCallback callback)
        {
            var form = Rest.GetWWWForm();
            form.Add("customName", name);

            StartCoroutine(Rest.EnqueuePut("LeaderboardEntry", id, form, callback: callback, debugOutput: true));
        }
        public GameObject ShowHighScoreTable(
            string leaderboardID,
            LeaderboardConfiguration configuration,
            int highlightRowID = -1,
            GameObject leaderboardPrefab = null,
            Canvas onCanvas = null,
            float height = -1,
            bool showCloseButton = true,
            KeyCode keyCodeForClose = KeyCode.None,
            HighScoreTable.CloseCallback closeCallback = null)
        {
            var prefab = leaderboardPrefab ?? PlayURPluginHelper.instance.defaultHighScoreTablePrefab;
            var canvas = onCanvas ?? FindObjectOfType<Canvas>();
            var go = Instantiate(prefab, canvas.transform);
            var highScoreTableScript = go.GetComponent<HighScoreTable>();
            highScoreTableScript.leaderboardID = leaderboardID;
            highScoreTableScript.configuration = configuration;
            highScoreTableScript.highlightRowID = highlightRowID;
            highScoreTableScript.showCloseButton = showCloseButton;
            highScoreTableScript.closeCallback = closeCallback;
            highScoreTableScript.height = height;
            if (keyCodeForClose != KeyCode.None)
                highScoreTableScript.useKeyCodeForClose = keyCodeForClose;
            highScoreTableScript.Init();
            return go;
        }
        public IEnumerator ShowHighScoreTableFor(
            float seconds,
            string leaderboardID,
            LeaderboardConfiguration configuration,
            int highlightRowID = -1,
            GameObject leaderboardPrefab = null,
            Canvas onCanvas = null,
            float height = -1,
            bool showCloseButton = false,
            KeyCode keyCodeForClose = KeyCode.Delete)
        {
            var go = ShowHighScoreTable(leaderboardID, configuration, highlightRowID, leaderboardPrefab, onCanvas, height, showCloseButton, keyCodeForClose);
            yield return new WaitForSecondsRealtime(seconds);
            Destroy(go);
        }
        public IEnumerator ShowHighScoreTableRoutine(
            string leaderboardID,
            LeaderboardConfiguration configuration,
            int highlightRowID = -1,
            GameObject leaderboardPrefab = null,
            Canvas onCanvas = null,
            float height = -1,
            bool showCloseButton = true,
            KeyCode keyCodeForClose = KeyCode.Delete)
        {
            var waiting = true;
            ShowHighScoreTable(leaderboardID, configuration, highlightRowID, leaderboardPrefab, onCanvas, height, showCloseButton, keyCodeForClose, closeCallback: () =>
            {
                waiting = false;
            });

            while (waiting)
                yield return new WaitForEndOfFrame();
        }
        public IEnumerator AddLeaderboardEntryAndShowHighScoreTableRoutine(
            string leaderboardID,
            float score,
            LeaderboardConfiguration configuration,
            GameObject leaderboardPrefab = null,
            Canvas onCanvas = null,
            float height = -1,
            bool showCloseButton = true,
            KeyCode keyCodeForClose = KeyCode.None,
            params object[] extraFields)
        {
            var waiting = true;
            int highlightRowID = -1;
            AddLeaderboardEntry(leaderboardID, score, configuration, callback: (succ, data) =>
            {
                waiting = false;
                if (data != null) int.TryParse(data["id"], out highlightRowID);
            }, extraFields);

            while (waiting)
                yield return new WaitForEndOfFrame();

            yield return StartCoroutine(ShowHighScoreTableRoutine(leaderboardID, configuration, highlightRowID, leaderboardPrefab, onCanvas, height, showCloseButton, keyCodeForClose));

        }
        #endregion

        #region Logout/Quit stuff
        /// <summary>
        /// Logout the current user. Will move them to the PlayURLogin scene. Clears PlayerPref data that stored their password.
        /// </summary>
        public void Logout()
        {
            if (UnityEngine.PlayerPrefs.HasKey(PERSIST_KEY_PREFIX + "password"))
            {
                UnityEngine.PlayerPrefs.DeleteKey(PERSIST_KEY_PREFIX + "password");
                UnityEngine.PlayerPrefs.Save();
            }

            SceneManager.LoadScene(0);
        }
        void Quit()
        {
            Application.Quit();
        }
        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            StartCoroutine(RecordActionRoutineInternal());
        }
        #endregion

        #region Popups
        //TODO: turn these into achievements
        //TODO: stack these visually like steam does, or queue them

        /// <summary>
        /// Shows a popup message with a given text and image. Requires that a popup prefab is set in the PlayURPluginHelper.
        /// </summary>
        /// <param name="text">The text to show in the popup.</param>
        /// <param name="image">The sprite image to show in the popup.</param>
        public void ShowCloseablePopup(string text, Sprite image = null)
        {
            ShowPopup(text, image, -1);
        }

        /// <summary>
        /// Shows a popup message with a given text and image for a given duration of time. Requires that a popup prefab is set in the PlayURPluginHelper.
        /// </summary>
        /// <param name="text">The text to show in the popup.</param>
        /// <param name="image">The sprite image to show in the popup.</param>
        /// <param name="duration">The duration of time to show the popup for.</param>
        public void ShowPopup(string text, Sprite image = null, float duration = 2) //if duration is -1 then popup needs to be closed
        {
            //TODO: handle null image
            var closeable = duration == -1;
            var go = Instantiate(PlayURPluginHelper.instance.defaultPopupPrefab);
            DontDestroyOnLoad(go);

            go.transform.Find("Popup/Image").GetComponent<Image>().sprite = image;
            go.GetComponentInChildren<Text>().text = text;

            var currentPopup = go.GetComponentInChildren<Animator>();

            var closeButton = go.GetComponentInChildren<Button>();
            if (closeable == false)
            {
                closeButton?.gameObject.SetActive(false);
                StartCoroutine(ClosePopupAfter(duration, currentPopup));
            }
            else
            {
                closeButton?.gameObject.SetActive(true);
                closeButton?.onClick.AddListener(() => ClosePopup(currentPopup));
            }
        }
        void ClosePopup(Animator popup)
        {
            if (popup)
            {
                popup.SetTrigger("Close");
                Destroy(popup.transform.parent.gameObject, 5);
            }
        }
        IEnumerator ClosePopupAfter(float duration, Animator popup)
        {
            yield return new WaitForSecondsRealtime(duration);
            ClosePopup(popup);
        }
        #endregion

        #region PlayerPrefs
        /// <summary>
        /// Saves playerprefs (overrides unity's ones) to the server.
        /// </summary>
        /// <param name="DATA">The data to save, should come from PlayerPrefs class</param>
        /// <param name="HTMLencode">Optionally convert form items special characters using <code>WebUtility.HtmlEncode</code>. </param>
        /// <param name="debugOutput">Optionally debug to the Unity console a bunch of information about how the request occurred. </param>
        public void SavePlayerPrefs(Dictionary<string, object> DATA, bool HTMLencode = false, bool debugOutput = false)
        {
            //Log("SavePlayerPrefs");
            var convertedData = Rest.GetWWWForm();
            foreach (var kvp in DATA)
            {
                var value = kvp.Value != null ? kvp.Value : "";
                convertedData.Add(kvp.Key, value.ToString());

                convertedData.Add("__type__" + kvp.Key, value.GetType().ToString());
            }

            StartCoroutine(Rest.EnqueuePost("SavePlayerPrefs", convertedData, null, HTMLencode: HTMLencode, debugOutput: debugOutput));
        }

        /// <summary>
        /// Loads playerprefs (overrides unity's ones) from the server.
        /// This code is automatically called on login.
        /// </summary>
        /// <param name="callback">Code to run once the player prefs have been loaded</param>
        public void LoadPlayerPrefs(Rest.ServerCallback callback)
        {
            //Log("LoadPlayerPrefs");
            var form = Rest.GetWWWForm();
            StartCoroutine(Rest.EnqueueGet("SavePlayerPrefs", form, callback, debugOutput: false));
        }
        #endregion

        #region Logs
        /// <summary>
        /// An enum representing the different Unity log severity levels. Used for retrieving logs later at a given level of severity.
        /// </summary>
        public enum LogLevel
        {
            Log,
            Warning,
            Error,
            Exception,
            Break
        }

        /// <summary>
        /// Retrieves a history of all the debug messages printed out thus far
        /// </summary>
        public string GetDebugLogs(LogLevel minimumLevel)
        {
            return Debug.GetDebugLog(minimumLevel);
        }

        /// <summary>
        /// Logs an object's details to the Unity console, but with [PlayUR] in front, so as to be able to isolate plugin messages in unity.
        /// Could be used in the future to display on-screen log messages
        /// </summary>
        /// <param name="o">The object to debug out</param>
        /// <param name="context">The context object that Unity uses for highlighting when you click on the log message (optional).</param>
        public static void Log(object o, UnityEngine.Object context = null)
        {
            if (o == null)
                Debug.Log("[PlayUR] NULL", context);
            else
                Debug.Log("[PlayUR] " + o.ToString(), context);
        }

        /// <summary>
        /// Logs an object's as an Error to the Unity console, but with [PlayUR] in front, so as to be able to isolate plugin messages
        /// </summary>
        /// <param name="o">The object to debug out</param>
        /// <param name="context">The context object that Unity uses for highlighting when you click on the log message (optional).</param>
        public static void LogError(object o, UnityEngine.Object context = null, bool breakCode = false)
        {
            if (o == null)
                Debug.LogError("[PlayUR] NULL", context);
            else
                Debug.LogError("[PlayUR] " + o.ToString(), context);

            if (breakCode)
            {
                Debug.Break();
            }
        }

        /// <summary>
        /// Logs an object's as an Warning to the Unity console, but with [PlayUR] in front, so as to be able to isolate plugin messages
        /// </summary>
        /// <param name="o">The object to debug out</param>
        /// <param name="context">The context object that Unity uses for highlighting when you click on the log message (optional).</param>
        public static void LogWarning(object o, UnityEngine.Object context = null)
        {
            if (o == null)
                Debug.LogWarning("[PlayUR] NULL", context);
            else
                Debug.LogWarning("[PlayUR] " + o.ToString(), context);
        }
        #endregion

        #region Utils
        /// <summary>
        /// Helpful function that gets the current time in MySQL-ready format.
        /// </summary>
        /// <returns>The current time (in our timezone) in <c>"yyyy-MM-dd HH:mm:ss"</c> format</returns>
        public string GetMysqlFormatTime()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Gets the history in a base64 encoded and compressed string.
        /// </summary>
        /// <returns></returns>
        private string GetHistoryString()
        {
            return Convert.ToBase64String(Rest.Queue.Save(System.IO.Compression.CompressionLevel.Fastest));
        }
        #endregion
    }
}

#region Exceptions
namespace PlayUR.Exceptions
{

    /// <summary>
    /// Thrown when a user attempts to open the game but they haven't been allocated to an ExperimentGroup
    /// and the allocation method for the Experiment doesn't result in them being auto-allocated.
    /// </summary>
    public class GameNotOwnedException : System.Exception
    {
        User user;
        int gameID;
        public GameNotOwnedException(User user, int gameID) { this.user = user; this.gameID = gameID; }
        public override string Message
        {
            get
            {
                return string.Format("No configuration found for user {0} for game id {1}. Likely causes: you haven't set up any experiments, or your experiment isn't configured to accept new users.", user.id, gameID);
            }
        }

    }


    /// <summary>
    /// Thrown when a user attempts to open the game but the game is unable to assign them to an
    /// Experiment Group, because all the groups are over capacity.
    /// </summary>
    public class ExperimentGroupsFullException : System.Exception
    {
        User user;
        int gameID;
        public ExperimentGroupsFullException(User user, int gameID) { this.user = user; this.gameID = gameID; }
        public override string Message
        {
            get
            {
                return string.Format("Could not assign user {0} to a group for game id {1}. Please check max users values for experiment groups.", user.id, gameID);
            }
        }

    }

    /// <summary>
    /// Thrown when attempting to access a configuration value but the <see cref="Configuration"/> has not yet been loaded.
    /// </summary>
    public class ConfigurationNotReadyException : System.Exception
    {
        public override string Message
        {
            get
            {
                return "Configuration was null. This could be caused by script execution order. Make sure you have pressed 'PlayUR->Set Up Plugin' in the Editor.";
            }
        }
    }

    /// <summary>
    /// Thrown when a parameter is requested from the <see cref="Configuration"/>, but was unable to be converted to the requested format (int/float/bool).
    /// </summary>
    public class InvalidParamFormatException : System.Exception
    {
        string key;
        System.Type format;
        public InvalidParamFormatException(string key, System.Type format) { this.key = key; this.format = format; }
        public override string Message
        {
            get
            {
                return string.Format("Parameter '{0}' was not in the requested format ({1}).", key, format.ToString());
            }
        }
    }

    /// <summary>
    /// Thrown when a parameter is requested from the <see cref="Configuration"/> but was not found in the configuration.
    /// </summary>
    public class ParameterNotFoundException : System.Exception
    {
        string key;
        public ParameterNotFoundException(string key) { this.key = key; }
        public override string Message
        {
            get
            {
                return string.Format("Parameter '{0}' not found.", key);
            }
        }
    }

    /// <summary>
    /// Thrown when the plugin is unable to talk to to the server (invalid response, no internet connection etc.)
    /// </summary>
    public class ServerCommunicationException : System.Exception
    {
        string msg;
        public ServerCommunicationException(string msg) { this.msg = msg; }
        public override string Message
        {
            get
            {
                return string.Format("Server error: {0}", msg);
            }
        }
    }

    /// <summary>
    /// Thrown when attempting to end a session but there is no active session.
    /// </summary>
    public class SessionNotStartedException : System.Exception
    {
        public override string Message
        {
            get
            {
                return "Cannot perform operation on Session as no Session has started";
            }
        }
    }

    /// <summary>
    /// Thrown when attempting to start a session but there is already an active session. 
    /// </summary>
    public class SessionAlreadyStartedException : System.Exception
    {
        public override string Message
        {
            get
            {
                return "Cannot perform operation on Session as Session has already started";
            }
        }
    }

    /// <summary>
    /// Thrown when attempting to use a leaderboard without a valid Leaderboard ID. 
    /// </summary>
    public class InvalidLeaderboardIDException : System.Exception
    {
        string id;
        public InvalidLeaderboardIDException(string id) { this.id = id; }
        public override string Message
        {
            get
            {
                return "Cannot perform leaderboard operation with leaderboard ID '" + id + "'";
            }
        }
    }
}
#endregion

#region PlayerPrefs
/// <summary>
/// An override of Unity's player prefs, needs to be in the top-level namespace.
/// </summary>
public static class PlayerPrefs
{
    public static Dictionary<string, object> DATA = new Dictionary<string, object>();

    public static void Save()
    {
        PlayUR.PlayURPlugin.instance.SavePlayerPrefs(DATA, debugOutput: false);
        UnityEngine.PlayerPrefs.Save();
    }
    public static void Load() { Load(null); }
    public static void Load(PlayUR.Core.Rest.ServerCallback callback)
    {
        PlayUR.PlayURPlugin.instance.LoadPlayerPrefs((succ, result) =>
        {
            var results = result["results"].AsArray;
            foreach (var r in results.Values)
            {
                var key = r["key"];
                var value = r["value"].Value;
                var type = r["type"];
                try
                {
                    var v = System.Convert.ChangeType(value, System.Type.GetType(type));
                    if (DATA.ContainsKey(key))
                        DATA[key] = v;
                    else
                        DATA.Add(key, v);
                }
                catch (System.Exception e) { PlayUR.PlayURPlugin.LogError("Failed to convert " + value + " to " + type + ". Exception:" + e.GetType() + " - " + e.Message); }

                if (callback != null) callback(succ, result);
            }
        });
    }
    public static IEnumerator PeriodicallySavePlayerPrefs(float interval = 5)
    {
        var i = new WaitForSeconds(interval);
        while (true)
        {
            yield return i;
            Save();
        }
    }

    public static void SetInt(string key, int v)
    {
        if (DATA.ContainsKey(key) == false)
            DATA.Add(key, v);
        else
            DATA[key] = v;

        UnityEngine.PlayerPrefs.SetInt(key, v);
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (DATA.ContainsKey(key))
            try
            {
                return (int)DATA[key];
            }
            catch (System.InvalidCastException) { return defaultValue; }
        else
            return defaultValue;
    }
    public static void SetString(string key, string v)
    {
        if (DATA.ContainsKey(key) == false)
            DATA.Add(key, v);
        else
            DATA[key] = v;

        UnityEngine.PlayerPrefs.SetString(key, v);
    }
    public static string GetString(string key, string defaultValue = "")
    {
        if (DATA.ContainsKey(key))
            try
            {
                return (string)DATA[key];
            }
            catch (System.InvalidCastException) { return defaultValue; }
        else
            return defaultValue;
    }
    public static void SetFloat(string key, float v)
    {
        if (DATA.ContainsKey(key) == false)
            DATA.Add(key, v);
        else
            DATA[key] = v;

        UnityEngine.PlayerPrefs.SetFloat(key, v);
    }
    public static float GetFloat(string key, float defaultValue = 0)
    {
        if (DATA.ContainsKey(key))
            try
            {
                return (float)DATA[key];
            }
            catch (System.InvalidCastException) { return defaultValue; }
        else
            return defaultValue;
    }
    public static void SetBool(string key, bool v)
    {
        if (DATA.ContainsKey(key) == false)
            DATA.Add(key, v);
        else
            DATA[key] = v;

        UnityEngine.PlayerPrefs.SetInt(key, v ? 1 : 0);
    }
    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (DATA.ContainsKey(key))
            try
            {
                return (bool)DATA[key];
            }
            catch (System.InvalidCastException) { return defaultValue; }
        else
            return defaultValue;
    }

    public static bool HasKey(string key)
    {
        return DATA.ContainsKey(key);
    }
    public static void DeleteKey(string key)
    {
        if (DATA.ContainsKey(key))
            DATA.Remove(key);

        UnityEngine.PlayerPrefs.DeleteKey(key);
    }

    public static void DeleteAll()
    {
        DATA = new Dictionary<string, object>();

        UnityEngine.PlayerPrefs.DeleteAll();
    }

    public static void Clear()
    {
        DeleteAll();
    }
}
#endregion

#region Debug Logging
/// <summary>
/// An override of Unity's Debug class, allowing us to capture debug messages and send them to the server before allowing the default Unity behaviour to take place.
/// </summary>
public static class Debug
{
    struct DebugMessage
    {
        public string timestamp;
        public PlayURPlugin.LogLevel level;
        public string message;
    }
    static List<DebugMessage> debug = new List<DebugMessage>();

    public static string GetDebugLog(PlayURPlugin.LogLevel minimumLevel, bool clear = true)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var d in debug)
        {
            if ((int)d.level >= (int)minimumLevel)
            {
                sb.AppendLine($"[{d.timestamp}] [{d.level}]: {d.message}");
            }
        }
        if (clear) debug.Clear();
        return sb.ToString();
    }
    static string GetTimestamp()
    {
        return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }

    public static void Log(object message)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Log, message = message.ToString() });
        UnityEngine.Debug.Log(message);
    }
    public static void Log(object message, UnityEngine.Object context)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Log, message = message.ToString() });
        UnityEngine.Debug.Log(message, context);
    }
    public static void LogError(object message)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Error, message = message.ToString() });
        UnityEngine.Debug.LogError(message);
    }
    public static void LogError(object message, UnityEngine.Object context)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Error, message = message.ToString() });
        UnityEngine.Debug.LogError(message, context);
    }
    public static void LogWarning(object message)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Warning, message = message.ToString() });
        UnityEngine.Debug.LogWarning(message);
    }
    public static void LogWarning(object message, UnityEngine.Object context)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Warning, message = message.ToString() });
        UnityEngine.Debug.LogWarning(message, context);
    }
    public static void LogException(System.Exception exception)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Exception, message = exception.ToString() });
        UnityEngine.Debug.LogException(exception);
    }
    public static void LogException(System.Exception exception, UnityEngine.Object context)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Exception, message = exception.ToString() });
        UnityEngine.Debug.LogException(exception, context);
    }
    public static void LogFormat(string format, params object[] args)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Exception, message = string.Format(format, args) });
        UnityEngine.Debug.LogFormat(format, args);
    }
    public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Exception, message = string.Format(format, args) });
        UnityEngine.Debug.LogFormat(context, format, args);
    }

    public static void Break()
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Break, message = "" });
        UnityEngine.Debug.Break();
    }

    public static void DebugBreak()
    {
        debug.Add(new DebugMessage { timestamp = GetTimestamp(), level = PlayURPlugin.LogLevel.Break, message = "" });
        UnityEngine.Debug.Break();
    }

    public static void ClearDeveloperConsole()
    {
        UnityEngine.Debug.ClearDeveloperConsole();
    }
}
#endregion