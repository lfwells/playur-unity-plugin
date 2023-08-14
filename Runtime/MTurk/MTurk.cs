using System.Collections;
using PlayUR.Core;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        public const string MTURK_URL_PARAM = "mTurkID";
        public const string MTURK_API_ENDPOINT = "MTurkCompletion";

        /// <summary>
        /// Determines if the game was launched with a MTurk ID defined.
        /// MTurk IDs are passed to the when launched in the web interface via the URL GET parameter defined in <see cref="MTURK_URL_PARAM"/>.
        /// When testing in the Unity Editor, this value can be overridden by setting <see cref="PlayURPluginHelper.forceMTurkIDInEditor"/>.
        /// </summary>
        public bool HasMTurkID 
        {
            get { return !string.IsNullOrEmpty(MTurkID); }
        }

        /// <summary>
        /// Obtains the MTurk ID if it exists.
        /// MTurk IDs are passed to the when launched in the web interface via the URL GET parameter defined in <see cref="MTURK_URL_PARAM"/>.
        /// When testing in the Unity Editor, this value can be overridden by setting <see cref="PlayURPluginHelper.forceMTurkIDInEditor"/>.
        /// Will return null if the game was not launched with a MTurk ID.
        /// </summary>
        public string MTurkID
        {
            get {
                #if UNITY_EDITOR
                    return PlayURPlugin.Settings.forceMTurkIDInEditor;
                #else
                    string result = null;
                    URLParameters.GetSearchParameters().TryGetValue(MTURK_URL_PARAM, out result);
                    return result;
                #endif
            }
        }

        int mTurkCompletionRowID;

        IEnumerator InitMTurk()
        {
            if (HasMTurkID)
            {
                var form = Rest.GetWWWFormWithExperimentInfo();
                form.Add(MTURK_URL_PARAM, MTurkID.ToString());
                
                yield return Rest.EnqueuePost(MTURK_API_ENDPOINT, form, (succ, result) =>
                {
                    if (succ)
                    {
                        mTurkCompletionRowID = result["id"].AsInt;
                        ShowMTurkStartPopup();
                    }
                    else
                    {
                        mTurkCompletionRowID = -1;
                    }
                }, debugOutput: true);
            }
            yield return 0;
        }

        /// <summary>
        /// Record on PlayUR that the MTurk participant has completed the game.
        /// If successful, shows a popup in the corner with the Amazon MTurk Logo (as defined in <see cref="PlayURPluginHelper.mTurkLogo"/>) and the message defined in <see cref="PlayURPluginHelper.mTurkCompletionMessage"/>.
        /// </summary>
        /// <exception cref="PlayUR.Exceptions.InvalidMTurkState">If the game was not launched with a MTurkID.</exception>
        public void MarkMTurkComplete()
        {
            if (HasMTurkID)
            {
                if (mTurkCompletionRowID == -1)
                {
                    throw new PlayUR.Exceptions.InvalidMTurkState();
                }
                else
                {
                    var form = Rest.GetWWWForm();
                    form.Add("complete", "1");

                    StartCoroutine(Rest.EnqueuePut(MTURK_API_ENDPOINT, mTurkCompletionRowID, form, (succ, result) =>
                    {
                        if (succ)
                        {
                            ShowMTurkCompletePopup();
                        }
                    }, debugOutput: true));
                }
            }
        }

        void ShowMTurkStartPopup()
        {
            ShowPopup("MTurk Participant:\n"+MTurkID, PlayURPlugin.Settings.mTurkLogo);
        }
        void ShowMTurkCompletePopup()
        {
            ShowCloseablePopup(PlayURPlugin.Settings.mTurkCompletionMessage, PlayURPlugin.Settings.mTurkLogo);
        }
    }

    
}
namespace PlayUR.Exceptions
{ 
    /// <summary>
    /// Thrown when a we try to set mturk as complete, but we have no row handle in database
    /// </summary>
    public class InvalidMTurkState : System.Exception
    {
        public override string Message
        {
            get
            {
                return "No MTurk completion row found. Did you forget to call InitMTurk? Or was there another initialisation error?";
            }
        }
    }
}