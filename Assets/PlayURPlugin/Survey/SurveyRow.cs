using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;


namespace PlayUR.Surveys
{
    /// <summary>
    /// A monobehaviour to go on the prefab for a survey row. 
    /// This is already done for the provided survey row prefab.
    /// </summary>
    public class SurveyRow : MonoBehaviour
    {
        [HideInInspector]
        /// <summary>
        /// The parent survey popup instance. Populated at runtime.
        /// </summary>
        public SurveyPopup popup;

        [HideInInspector]
        /// <summary>
        /// The associated survey question scriptable objct. Populated at runtime.
        /// </summary>
        public SurveyQuestion surveyQuestion;

#pragma warning disable 649
        [SerializeField]
        private Text questionText;
        
        private ToggleGroup toggleGroup;
        private ToggleGroup[] toggleGroups;
#pragma warning restore 649

        public Color backgroundColor;
        public Color backgroundColorHighlighted;

        private List<Toggle> toggles = new List<Toggle>();

        /// <summary>
        /// Populate the survey row with the given question.
        /// </summary>
        /// <param name="question">The question scriptable object data.</param>
        public void Fill(SurveyQuestion question)
        {
            surveyQuestion = question;
            questionText.text = question.question;

            toggleGroups = GetComponentsInChildren<ToggleGroup>(includeInactive:true);
            toggleGroup = toggleGroups[(int)question.labels];

            foreach (var tg in toggleGroups)
            {
                tg.gameObject.SetActive(tg == toggleGroup);
            }

            InitToggleGroup();
        }

        void InitToggleGroup()
        {
            foreach (Toggle toggle in toggleGroup.GetComponentsInChildren<Toggle>())
            {
                toggle.isOn = false;
                toggle.onValueChanged.AddListener(OptionSelected);
                toggles.Add(toggle);
            }
        }

        /// <summary>
        /// Get the selected response for this question.
        /// </summary>
        /// <returns>The selected response index, or -1 for no selection.</returns>
        public int IndexSelected()
        {
            for (int i = 0; i < toggles.Count; i++)
            {
                if (toggles[i].isOn) return i;
            }

            return -1;
        }

        void OptionSelected(bool value)
        {
            if (value)
                popup.onResponse.Invoke(surveyQuestion.survey, surveyQuestion, surveyQuestion.AnalyticsKey, IndexSelected());
        }

        /// <summary>
        /// Save the value to PlayUR analytics. 
        /// </summary>
        /// <param name="action"></param>
        public void Save(Action action)
        {
            PlayURPlugin.Log($"Survey OptionSaved() {surveyQuestion.AnalyticsKey} = {questionText.text} {IndexSelected()}");

            var data = PlayURPlugin.instance.BeginRecordAction(action);
            data.AddColumn(surveyQuestion.survey.analyticsQuestionColumn, surveyQuestion.AnalyticsKey);
            data.AddColumn(surveyQuestion.survey.analyticsAnswerColumn, IndexSelected());
            data.Record();
        }
    }
}
