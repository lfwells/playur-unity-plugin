using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Diagnostics;
using JetBrains.Annotations;
using System.Collections;

namespace PlayUR.Core
{
    /// <summary>
    /// Singleton in charge of displaying the login screen. 
    /// GUI not actually used on the webpage, but allows standalone exes to integrate with the system.
    /// Login system on the webpage actually auto-logs in using some functions in this class.
    /// </summary>
    public class PlayURLoginCanvas : UnitySingleton<PlayURLoginCanvas>
    {

        #region Constants
#if UNITY_EDITOR || !UNITY_WEBGL
        bool ENABLE_PERSISTENCE = true;
#else
        bool ENABLE_PERSISTENCE = false;
#endif
        #endregion

        #region GUI Links
        public InputField username, password;
        public Text feedback;
        public Button submit, register, loginWithBrowser, loginWithBrowser2, loginPassword, register2;
        public GameObject loginScreen, registerScreen;
        public GameObject fullscreenError;
        public Text errorText, errorTitle;
        public Button errorOK;

        public GameObject panelBrowser, panelLogin;

        public InputField registerUsername, registerPassword, registerConfirmPassword, registerEmail, registerFirstName, registerLastName;
        public Button registerSubmit, registerCancel;
        public Text registerFeedback;
        #endregion

        #region State and Set Up
        /// <summary>
        /// Returns if we have successfully logged in or not
        /// </summary>
        public static bool LoggedIn { get; set; }

        //this var either contains the text put in the password field, or is populated by the auto-login process.
        string usr;
        string pwd;

        //should we attempt to auto login? turn this flag off once we fail on an auto
        static bool autoLogin = true;
        //use this to keep a message between loads of the login scene
        static string persistFeedbackMessage = "";

        void Awake()
        {
            GetComponent<CanvasGroup>().alpha = 0;

            if (PlayURPlugin.Settings.fullScreenMode == PlayURSettings.FullScreenStartUpMode.AlwaysStartInFullScreen)
            {
                Screen.fullScreen = true;
            }
            else if (PlayURPlugin.Settings.fullScreenMode == PlayURSettings.FullScreenStartUpMode.AlwaysStartWindowed)
            {
                Screen.fullScreen = false;
            }

            if (PlayURPlugin.instance.IsDetachedMode)
            {
                PlayURPlugin.instance.DetachedModeProxy.LoginCanvasAwake(this);
            }
        }
        private void Start()
        {
            if (!string.IsNullOrEmpty(persistFeedbackMessage))
                feedback.text = persistFeedbackMessage;

            submit.onClick.AddListener(() => { usr = username.text;  pwd = password.text; Login(); });
            register.onClick.AddListener(() => { OpenRegister(); });
            register2.onClick.AddListener(() => { OpenRegister(); });
            registerCancel.onClick.AddListener(() => { CloseRegister(); });
            registerSubmit.onClick.AddListener(() => { Register(); });

            loginWithBrowser.onClick.AddListener(() => new PlayURLoginWebServer(StandaloneLogin));
            loginWithBrowser2.onClick.AddListener(() => new PlayURLoginWebServer(StandaloneLogin));

            loginPassword.onClick.AddListener(() => { panelLogin.SetActive(true); panelBrowser.SetActive(false); });

            InitExperimentSelect();

            if (ENABLE_PERSISTENCE && autoLogin)
            {
                if (UnityEngine.PlayerPrefs.HasKey(PlayURPlugin.PERSIST_KEY_PREFIX + "username"))
                {
                    username.text = UnityEngine.PlayerPrefs.GetString(PlayURPlugin.PERSIST_KEY_PREFIX + "username");
                    usr = username.text;
                }

                if (UnityEngine.PlayerPrefs.HasKey(PlayURPlugin.PERSIST_KEY_PREFIX + "password"))
                {
                    pwd = UnityEngine.PlayerPrefs.GetString(PlayURPlugin.PERSIST_KEY_PREFIX + "password");
                    if (pwd.StartsWith("___TOKEN") == false)
                    {
                        PlayURPlugin.Log("Auto-login...");
                        Login();
                    }
                }
            }

            #if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                RequestWebGLLogin();
            } catch (System.EntryPointNotFoundException) { }
            #else
            GetComponent<CanvasGroup>().alpha = 1;
            #endif
        }

        bool scheduleALoginOnNextFrame = false; //used to return to main thread on server info obtained
        private void Update()
        {
            if (scheduleALoginOnNextFrame && Application.isFocused)
            {
                scheduleALoginOnNextFrame = false;
                Login();
            }
            CheckForAndEnableExperimentSelectButton();
        }
        /// <summary>
        /// Triggers a login request with whatever username and password has been entered.
        /// </summary>
        public void Login()
        {
            feedback.text = "Logging in... ";
            if (ENABLE_PERSISTENCE)
            {
                UnityEngine.PlayerPrefs.SetString(PlayURPlugin.PERSIST_KEY_PREFIX + "username", usr);
            }

            PlayURPlugin.instance.Login(usr, pwd, (succ, result) =>
            {
                password.text = string.Empty;
                PlayURPlugin.Log("Login Success: "+ succ);
                if (succ)
                {
                    //TODO: security ??
                    if (ENABLE_PERSISTENCE && (result["token_login"]?.AsBool ?? false) == false)
                    {
                        UnityEngine.PlayerPrefs.SetString(PlayURPlugin.PERSIST_KEY_PREFIX + "password", pwd);
                    }
                    if (ENABLE_PERSISTENCE && result["username"] != null)
                    {
                        UnityEngine.PlayerPrefs.SetString(PlayURPlugin.PERSIST_KEY_PREFIX + "username", result["username"].Value);
                    }
                    LoggedIn = true;
                    GoToNextScene();
                }
                else
                {
                    GetComponent<CanvasGroup>().alpha = 1;
                    feedback.text = "Incorrect Username or Password";//todo pull from server?
                }
            });
        }
        public void GoToNextScene()
        {
            if (PlayURPluginHelper.startedFromScene > 0)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(PlayURPluginHelper.startedFromScene);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            }
        }
        public void CancelLogin(string message = "Could not login. Contact the researcher.")
        {
            LoggedIn = false;
            autoLogin = false;
            persistFeedbackMessage = message;
        }

        void OpenRegister()
        {
            loginScreen.SetActive(false);
            registerScreen.SetActive(true);
            registerUsername.text = username.text;
            registerPassword.text = password.text;
            registerConfirmPassword.text = password.text;

            registerUsername.Select();
            registerUsername.ActivateInputField();
        }
        void CloseRegister()
        {
            loginScreen.SetActive(true);
            registerScreen.SetActive(false);

            username.Select();
            username.ActivateInputField();
        }


        /// <summary>
        /// Triggers a register request with whatever details has been entered.
        /// </summary>
        public void Register()
        {
            registerFeedback.text = "Registering... ";

            if (registerUsername.text.Length == 0)
            {
                registerFeedback.text = "No username entered";
            }
            else if (registerEmail.text.Length == 0)
            {
                registerFeedback.text = "No email entered";
            }
            else if (registerPassword.text != registerConfirmPassword.text)
            {
                registerFeedback.text = "Passwords entered do not match!";
            }
            else if (registerPassword.text.Length < 3)
            {
                registerFeedback.text = "Password too short!";
            }
            else
            {

                PlayURPlugin.instance.Register(registerUsername.text, registerPassword.text, registerEmail.text, registerFirstName.text, registerLastName.text, (succ, result) =>
                {
                    PlayURPlugin.Log("Register Success: " + succ+"'"+ result["message"]+"'");
                    if (succ)
                    {
                        username.text = registerUsername.text;
                        password.text = registerPassword.text;
                        usr = registerUsername.text;
                        pwd = password.text;
                        registerScreen.SetActive(false);
                        Login();
                    }
                    else
                    {
                        GetComponent<CanvasGroup>().alpha = 1;
                        registerFeedback.text = "Registration Failed: "+ result["message"];
                    }
                });
            }
        }
        #endregion

        #region WebGL and Standalone Linkage

#if UNITY_EDITOR || UNITY_WEBGL
        /// <summary>
        /// This function has a matching JavaScript function on the website which gets called when we call this function from C#
        /// Slightly convoluated set up uses this, as the webpage otherwise doesn't know when the <see cref="PlayURLoginCanvas"/>
        /// is ready to call <see cref="WebGLLogin(string)"/>. 
        /// </summary>
        [DllImport("__Internal")]
        private static extern void RequestWebGLLogin();
#else
        private static void RequestWebGLLogin() { }
#endif

        /// <summary>
        /// The website will call this function (inside <see cref="RequestWebGLLogin"/> in JavaScript).
        /// </summary>
        /// <param name="jsonInput">The username and password of the user.</param>
        public void WebGLLogin(string jsonInput)
        {
            var jsonData = JSON.Parse(jsonInput);
#if UNITY_WEBGL
            username.text = jsonData["username"];
#endif
            usr = jsonData["username"];
            pwd = jsonData["password"];

            PlayURPlugin.browserInfo = jsonData["browserInfo"];
            
            var e = jsonData["experiment"];
            int i = -1;
            int.TryParse(e, out i);
            if (i != -1)
            {
                PlayURPlugin.instance.didRequestExperiment = true;
                PlayURPlugin.instance.requestedExperiment = (Experiment)i;
            }
            e = jsonData["experimentGroup"];
            i = -1;
            int.TryParse(e, out i);
            if (i != -1)
            {
                PlayURPlugin.instance.didRequestExperimentGroup = true;
                PlayURPlugin.instance.requestedExperimentGroup = (ExperimentGroup)i;
            }
            scheduleALoginOnNextFrame = true;
        }

        /// <summary>
        /// Function called by standalone builds upon callback from PlayUR login page
        /// </summary>
        /// <param name="jsonInput">The username and password of the user.</param>
        public void StandaloneLogin(string authInput)
        {
            usr = "___TOKEN";
            pwd = "___TOKEN" + authInput;

            scheduleALoginOnNextFrame = true;
        }
        #endregion

        #region Error Display
        /// <summary>
        /// Displays a full-screen error, and prevents user from leaving this scene
        /// </summary>
        /// <param name="message">Body text of error popup</param>
        /// <param name="title">Title text of error popup</param>
        /// <param name="showOK">Show an OK button which user can press to try and log in again?</param>
        public void ShowError(string message, string title = "Error", bool showOK = false)
        {
            fullscreenError.SetActive(true);
            errorOK.onClick.AddListener(() => {
                fullscreenError.SetActive(false);
            });
            errorOK.gameObject.SetActive(showOK);
            errorTitle.text = title;
            errorText.text = message;
        }
        #endregion

        #region Experiment Select
        public Button experimentSelect, experimentSelectClose;
        public GameObject panelExperimentSelect;

        void InitExperimentSelect()
        {
            experimentSelect.onClick.AddListener(() => { panelExperimentSelect.SetActive(true); });
            experimentSelectClose.onClick.AddListener(() => { panelExperimentSelect.SetActive(false); });
        }
        void CheckForAndEnableExperimentSelectButton()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        experimentSelect.gameObject.SetActive(true);
                    }
                }
            }
        }
        #endregion
    }



}