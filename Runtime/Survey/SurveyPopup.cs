using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

using System.Collections.Generic;

namespace PlayUR.Surveys
{
    /// <summary>
    /// This class contains the logic associated to the popup for displaying a survey.
    /// This monobehaviour should be placed on a survey popup prefab. This is already done for the provided survey popup prefab.
    /// </summary>
    public class SurveyPopup : MonoBehaviour
    {
        /// <summary>
        /// Unity events to run when the survey is opened.
        /// </summary>
        public SurveyPopupEvent onOpen;
        /// <summary>
        /// Unity events to run when the survey changes pages.
        /// </summary>
        public SurveyPopupEvent onNextPage;
        /// <summary>
        /// Unity events to run when the survey is closed.
        /// </summary>
        public SurveyPopupEvent onClose;
        /// <summary>
        /// Unity events to run when the user responds to a question.
        /// </summary>
        public SurveyPopupResponseEvent onResponse;

#pragma warning disable 649
        [SerializeField]
        private RectTransform itemsParent;
        private VerticalLayoutGroup layoutGroup;

        [SerializeField]
        private Button nextButton;
        private Text nextButtonText;


        [SerializeField]
        private Text titleTextBox, instructionTextBox;
#pragma warning restore 649

        /// <summary>
        /// The scriptable object representing the survey to show.
        /// </summary>
        public Survey survey;

        private List<SurveyRow> questions = new List<SurveyRow>();

        void Start()
        {
            nextButtonText = nextButton.GetComponentInChildren<Text>();
            layoutGroup = GetComponentInChildren<VerticalLayoutGroup>();

            titleTextBox.text = survey.titleText;
            instructionTextBox.text = survey.instructionsText;
            FillQuestions();

            onOpen.Invoke(survey);
        }

        void FillQuestions(int startIndex = 0)
        {
            startIndexCache = startIndex;

            if (survey.questionsPerPage < 0) survey.questionsPerPage = survey.questions.Count;

            for (int i = startIndex; i < startIndex + survey.questionsPerPage; i++)
            {
                if (i >= survey.questions.Count)
                {
                    break;
                }

                var question = survey.questions[i];
                question.survey = survey;

                var rowPrefab = PlayURPlugin.Settings.defaultSurveyRowPrefab;
                if (survey.rowPrefab != null) rowPrefab = survey.rowPrefab;

                var row = Instantiate(rowPrefab);
                row.transform.SetParent(itemsParent, false);
                var s = itemsParent.sizeDelta;
                s.y += row.GetComponent<RectTransform>().sizeDelta.y + layoutGroup.spacing;
                itemsParent.sizeDelta = s;
                var rowUI = row.GetComponent<SurveyRow>();
                rowUI.Fill(question);
                rowUI.popup = this;
                //index++;

                this.questions.Add(rowUI);
            }

            if (startIndex + survey.questionsPerPage >= survey.questions.Count)
            {
                allQuestionsAsked = true;
            }

            if (AllQuestionsAsked())
            {
                nextButtonText.text = survey.finishText;
            }
            else
            {
                nextButtonText.text = survey.nextPageText;
            }
        }

        bool allQuestionsAsked = false;
        int startIndexCache = 0;


        void Update()
        {
            nextButton.interactable = AllQuestionsAnsweredThisPage();
        }

        bool AllQuestionsAsked()
        {
            return allQuestionsAsked;
        }

        bool AllQuestionsAnsweredThisPage()
        {
            foreach (var row in questions)
            {
                if (row.IndexSelected() < 0) return false;
            }

            return true;
        }

        public void OnCloseButtonPressed()
        {
            foreach (var row in questions)
            {
                row.Save(survey.analyticsActionToRecordWith);
            }

            onNextPage.Invoke(survey);

            if (AllQuestionsAsked())
            {
                Destroy(gameObject);
                onClose.Invoke(survey);
            }
            else
            {
                foreach (var row in questions)
                {
                    Destroy(row.gameObject);
                }
                questions.Clear();

                FillQuestions(startIndexCache + survey.questionsPerPage);
            }
        }
    }

    [System.Serializable]
    public class SurveyPopupEvent : UnityEvent<Survey> { }
    [System.Serializable]
    public class SurveyPopupResponseEvent : UnityEvent<Survey, SurveyQuestion, string, int> { }
}
