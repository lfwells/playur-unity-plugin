using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.Surveys
{
    [CreateAssetMenu(menuName = "PlayUR/Survey Question")]
    /// <summary>
    /// Represents a single question within a survey.
    /// </summary>
    public class SurveyQuestion : ScriptableObject
    {
        /// <summary>
        /// Used for defining different types of likert responses
        /// </summary>
        public enum OptionLabels
        {
            //Custom,//TODO: custom option labels
            StronglyDisagreeToStronglyAgree5,
            NotAtAllToExtremely,
            //StronglyDisagreeToStronglyAgree7,
            //NeverToAlways4,
        }

        /// <summary>
        /// The text of the question
        /// </summary>
        public string question;

        /// <summary>
        /// What type of responses should be shown?
        /// </summary>
        public OptionLabels labels;

        [Tooltip("Leave blank to default to question label")]
        /// <summary>
        /// In PlayUR Analytics, what should be put into the Survey Question column?
        /// </summary>
        public string analyticsKey;

        /// <summary>
        /// In PlayUR Analytics, what should be put into the Survey Question column?
        /// Used internally by PlayUR to clean the value provided in the inspector.
        /// </summary>
        public string AnalyticsKey
        {
            get
            {
                if (string.IsNullOrEmpty(analyticsKey))
                    return question.Replace(',', ' ');
                return analyticsKey;
            }
        }

        [System.NonSerialized]
        /// <summary>
        /// What survey is this a question for. Populated by PlayUR at runtime.
        /// </summary>
        public Survey survey;
    }
}