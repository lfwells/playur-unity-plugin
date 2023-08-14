using System.Collections;
using UnityEngine;
using PlayUR.Surveys;

namespace PlayUR
{
    public partial class PlayURPlugin : UnitySingletonPersistent<PlayURPlugin>
    {
        /// <summary>
        /// Display a survey to the user.
        /// </summary>
        /// <param name="survey">The survey scriptable object defining the survey to show.</param>
        /// <returns>A reference to the instantiated survey popup gameobject.</returns>
        public SurveyPopup ShowSurveyPopup(Survey survey)
        {
            var prefab = PlayURPlugin.Settings.defaultSurveyPopupPrefab;
            if (survey.popupPrefab != null) prefab = survey.popupPrefab;

            var surveyGOInstance = Instantiate(prefab);
            var surveyInstance = surveyGOInstance.GetComponentInChildren<SurveyPopup>();
            surveyInstance.survey = survey;

            return surveyInstance;
        }

        /*public SurveyPopup ShowSurveyPopupAsNewScene(Survey settings)
        {
            //TODO: empty scene with canvas
            Canvas c;
            return Show(settings, c);
        }*/

        /// <summary>
        /// Display a survey to the user and wait for it to be completed. To be called within StartCoroutine.
        /// </summary>
        /// <param name="survey">The survey scriptable object defining the survey to show.</param>
        public IEnumerator ShowSurveyPopupWithCoroutine(Survey survey)
        {
            var popup = ShowSurveyPopup(survey);
            var finished = false;
            popup.onClose.AddListener((s) =>
            {
                finished = true;
            });
            while (finished == false)
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }
}