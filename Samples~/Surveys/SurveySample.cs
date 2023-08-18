using UnityEngine;
using PlayUR.Surveys;

namespace PlayUR.Samples
{
    /// <summary>
	/// Sample to show how to popup a survey. Data is set in a scriptable object in the sample.
	/// </summary>
    public class SurveySample : MonoBehaviour
    {
        public Survey survey;

        public void DoSurvey()
        {
            PlayURPlugin.instance.ShowSurveyPopup(survey);
            //note there is also a coroutine version
        }
    }
}
