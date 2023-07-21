using UnityEngine;
using PlayUR.Surveys;

namespace PlayUR.Samples
{
    public class SurveyTest : MonoBehaviour
    {
        public Survey survey;

        public void DoSurvey()
        {
            PlayURPlugin.instance.ShowSurveyPopup(survey);
            //note there is also a coroutine version
        }
    }
}
