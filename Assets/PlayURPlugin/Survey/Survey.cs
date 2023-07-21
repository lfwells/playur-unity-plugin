using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Surveys
{
    [CreateAssetMenu(menuName = "PlayUR/Survey")]
    /// <summary>
    /// Represents a survey that can be shown to the user.
    /// To save survey data, we use PlayUR Analytics. Be sure to set the <see cref="analyticsActionToRecordWith"/>, <see cref="analyticsQuestionColumn"/>, and <see cref="analyticsAnswerColumn"/> values as these refer to the generated enums for your game.
    /// </summary>
    public class Survey : ScriptableObject
    {
        /// <summary>
        /// The title to appear at the top of the survey.
        /// </summary>
        public string titleText = "Survey";

        /// <summary>
        /// The instructions to appear at the top of the survey.
        /// </summary>
        public string instructionsText = "Please indicate how much you agree with each statement below.";

        /// <summary>
        /// The text to appear on the next page button (if shown).
        /// </summary>
        public string nextPageText = "Next Page";

        /// <summary>
        /// The text to appear on th finish survey button.
        /// </summary>
        public string finishText = "Finish Survey";

        /// <summary>
        /// The questions to include in the survey.
        /// </summary>
        public List<SurveyQuestion> questions;

        [Tooltip("Leave as -1 for no pages")]
        /// <summary>
        /// How many questions should be shown per page?
        /// Use sentinel value of -1 to not use pages.
        /// </summary>
        public int questionsPerPage = -1;

        [Tooltip("Leave blank for default")]
        /// <summary>
        /// What prefab should be instantiated to show the survey content?
        /// Leave blank to use the default PlayUR popup prefab
        /// </summary>
        public GameObject popupPrefab;

        [Tooltip("Leave blank for default")]
        /// <summary>
        /// What prefab should be instantiated for each survey question?
        /// Leave blank to use the default PlayUR Survey Row prefab.
        /// </summary>
        public GameObject rowPrefab;

        /// <summary>
        /// Specify the action to record for each survey row as defined on the PlayUR back-end.
        /// </summary>
        public Action analyticsActionToRecordWith;

        /// <summary>
        /// Specify which column of the <see cref="analyticsActionToRecordWith"/> should contain the survey question text.
        /// </summary>
        public AnalyticsColumn analyticsQuestionColumn;

        /// <summary>
        /// Specify which column of the <see cref="analyticsActionToRecordWith"/> should contain the survey answer text.
        /// </summary>
        public AnalyticsColumn analyticsAnswerColumn;
    }
}