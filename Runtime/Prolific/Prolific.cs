using System.Collections;
using System.Diagnostics;
using PlayUR.Core;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        const string PROLIFIC_URL_PARAM = "prolificID";
        const string PROLIFIC_API_ENDPOINT = "ProlificCompletion";

        /// <summary>
        /// Determines if the game was launched with a Prolific ID defined.
        /// Prolific IDs are passed to the when launched in the web interface via the URL GET parameter defined in <see cref="PROLIFIC_URL_PARAM"/>.
        /// When testing in the Unity Editor, this value can be overridden by setting <see cref="PlayURPluginHelper.forceProlificIDInEditor"/>.
        /// </summary>
        public bool HasProlificID
        {
            get { return !string.IsNullOrEmpty(ProlificID); }
        }

        /// <summary>
        /// Obtains the Prolific ID if it exists.
        /// Prolific IDs are passed to the when launched in the web interface via the URL GET parameter defined in <see cref="PROLIFIC_URL_PARAM"/>.
        /// When testing in the Unity Editor, this value can be overridden by setting <see cref="PlayURPluginHelper.forceProlificIDInEditor"/>.
        /// Will return null if the game was not launched with a Prolific ID.
        /// </summary>
        public string ProlificID
        {
            get
            {
                string result = null;
#if UNITY_EDITOR
                result = PlayURPlugin.Settings.forceProlificIDInEditor;
#else
                URLParameters.GetSearchParameters().TryGetValue(PROLIFIC_URL_PARAM, out result);
#endif
                if (string.IsNullOrEmpty(result))
                {
                    result = prolificFromStandaloneLoginInfo;
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = PlayURPlugin.instance.configuration?.prolificID;
                }
                return result;
            }
        }

        protected string prolificFromStandaloneLoginInfo = null; //if in a standalone build, we can get the prolific from the login info (api/Configuration/index.php returns this)

        int prolificCompletionRowID;

        IEnumerator InitProlific()
        {
            if (HasProlificID)
            {
                if (IsDetachedMode)
                {
                    yield return StartCoroutine(DetachedModeProxy.InitProlific(this));
                    yield break;
                }

                var form = Rest.GetWWWFormWithExperimentInfo();
                form.Add(PROLIFIC_URL_PARAM, ProlificID.ToString());
                Debug.Log("Submitting Prolific init with ID: " + ProlificID.ToString());

                yield return Rest.EnqueuePost(PROLIFIC_API_ENDPOINT, form, (succ, result) =>
                {
                    if (succ)
                    {
                        prolificCompletionRowID = result["id"].AsInt;
                        ShowProlificStartPopup();
                    }
                    else
                    {
                        prolificCompletionRowID = -1;
                    }
                }, debugOutput: true);
            }
            yield return 0;
        }

        /// <summary>
        /// Record on PlayUR that the Prolific participant has completed the game.
        /// If successful, shows a popup in the corner with the Prolific Logo and the message defined in <see cref="PlayURSettings.prolificCompletionMessage"/>.
        /// </summary>
        /// <exception cref="PlayUR.Exceptions.InvalidProlificState">If the game was not launched with a Prolific ID.</exception>
        public void MarkProlificComplete()
        {
            if (HasProlificID)
            {
                if (IsDetachedMode)
                {
                    DetachedModeProxy.MarkProlificComplete(this);
                    return;
                }

                if (prolificCompletionRowID == -1)
                {
                    throw new PlayUR.Exceptions.InvalidProlificState();
                }
                else
                {
                    var form = Rest.GetWWWForm();
                    form.Add("complete", "1");

                    StartCoroutine(Rest.EnqueuePut(PROLIFIC_API_ENDPOINT, prolificCompletionRowID, form, (succ, result) =>
                    {
                        if (succ)
                        {
                            ShowProlificCompletePopup();
                        }
                    }, debugOutput: true));
                }
            }
        }

        //update: no longer showing this, as we dont store id
        void ShowProlificStartPopup()
        {
            ShowPopup(PlayURPlugin.Settings.prolificStartMessage, PlayURPlugin.Settings.prolificLogo);
        }
        void ShowProlificCompletePopup()
        {
            ShowCloseablePopup(PlayURPlugin.Settings.prolificCompletionMessage, PlayURPlugin.Settings.prolificLogo);
        }
        public void prolificCompletionCodeCopiedMessage()
        {
            ShowCloseablePopup(PlayURPlugin.Settings.prolificCompletionCodeCopiedMessage, PlayURPlugin.Settings.prolificLogo);
        }
    }


}

namespace PlayUR.Exceptions
{
    /// <summary>
    /// Thrown when a we try to set prolific as complete, but we have no row handle in database
    /// </summary>
    public class InvalidProlificState : System.Exception
    {
        public override string Message
        {
            get
            {
                return "No Prolific completion row found. Did you forget to call InitProlific? Or was there another initialisation error?";
            }
        }
    }
}