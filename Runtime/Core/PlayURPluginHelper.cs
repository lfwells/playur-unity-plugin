using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayUR.Core;
using System.Diagnostics;

namespace PlayUR
{
    /// <summary>
    /// This class goes on a prefab which is popped into any scene which you might want to immediately run in the editor from.
    /// Having this prefab in your scene will mean that on entering play mode the game will automatically redirect to the login scene and attempt to auto-login.
    /// </summary>
    public class PlayURPluginHelper : MonoBehaviour
    {
        public static PlayURPluginHelper instance;

        //used by the editor only
        public static int startedFromScene = -1;

        #region Domain Reloading Fix
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void InitDomainReload()
        {
            instance = null;
            startedFromScene = -1;
        }
        #endregion


        /// <summary>
        /// Includes a variant on the singleton pattern, begins session tracking if we are logged in.
        /// Otherwise kicks off the login process in-editor
        /// </summary>
        private void Awake()
        {
            //when we return the scene, a new plugin helper will appear, it should die straight away else we loop!
            if (instance != null)
            {
                DestroyImmediate(this.gameObject);
                return;
            }

            gameObject.AddComponent<URLParameters>();

            instance = this;
            DontDestroyOnLoad(this);
            StartCoroutine(Init());
        }

        /// <summary>
        /// Takes us to the login scene and returns us to the scene we came from as required.
        /// In builds, we will always get to the login scene first, then head on to Scene #1.
        /// </summary>
        IEnumerator Init()
        {
            //store current scene so we can return later
            var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            startedFromScene = currentSceneIndex;

            //if already on the login page, can  skip login
            if (currentSceneIndex == 0)
            {
                startedFromScene = 0;
            }
            else
            {
                //login (if required...)
                while (PlayURLoginCanvas.LoggedIn == false)
                {
                    yield return PlayURPlugin.GetLogin();
                }
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (PlayURPlugin.Settings.standardSessionTracking && gameObject.GetComponent<PlayURSessionTracker>() == null)
            {
                gameObject.AddComponent<PlayURSessionTracker>();
            }

            //definitely load the scene we were in in unity
            if (SceneManager.GetActiveScene().buildIndex != currentSceneIndex)
            {
                SceneManager.LoadScene(currentSceneIndex);
            }



            //no longer need this object
            //DestroyImmediate(this.gameObject);
        }
    }
}