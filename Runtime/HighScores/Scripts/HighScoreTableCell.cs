using UnityEngine;
using UnityEngine.UI;
using System;

namespace PlayUR
{
    /// <summary>
    /// Used to represent a single column within a highscore table row.
    /// This MonoBehaviour should be placed on a high-score table cell prefab. This script is already included in the provided highscore table cell prefab in PlayURPlugin/HighScores.
    /// </summary>
    public class HighScoreTableCell : MonoBehaviour
    {
        /// <summary>
        /// What text field should be used to display the text of this cell?
        /// </summary>
        public TextOrTMP textField;

        [Tooltip("Optional, only for editable rows")]
        /// <summary>
        /// What input field should be used to edit the text of this cell?
        /// </summary>
        public InputFieldOrTMP inputField;

        /// <summary>
        /// Used internally by PlayUR.
        /// </summary>
        public virtual void CreateCell(string text, PlayURPlugin.LeaderboardConfiguration configuration, bool nameEntry = false, bool isValue = false)
        {
            if (isValue)
            {
                switch (configuration.dataType)
                {
                    default:
                        if (string.IsNullOrEmpty(configuration.displayFormat) || configuration.displayFormat == "{0}")
                        {
                            textField.text = text;
                        }
                        else
                        {
                            textField.text = string.Format(configuration.displayFormat, text);
                        }
                        break;

                    case PlayURPlugin.LeaderboardConfiguration.DataType.TimeSeconds:
                        int totalSeconds = 0;
                        int.TryParse(text, out totalSeconds);
                        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
                        int mm = timeSpan.Minutes;
                        int ss = timeSpan.Seconds;

                        if (string.IsNullOrEmpty(configuration.displayFormat) || configuration.displayFormat == "{0}")
                        {
                            textField.text = string.Format("{0}m {1}s", mm, ss);
                        }
                        else
                        {
                            textField.text = string.Format(configuration.displayFormat, mm, ss);
                        }
                        break;

                    case PlayURPlugin.LeaderboardConfiguration.DataType.TimeMilliseconds:
                        int totalMilliSeconds = 0;
                        int.TryParse(text, out totalMilliSeconds);
                        var timeSpan2 = TimeSpan.FromMilliseconds(totalMilliSeconds);
                        int mm2 = timeSpan2.Minutes;
                        int ss2 = timeSpan2.Seconds;
                        int ms = timeSpan2.Milliseconds;

                        if (string.IsNullOrEmpty(configuration.displayFormat) || configuration.displayFormat == "{0}")
                        {
                            textField.text = string.Format("{0}m {1}.{2}s", mm2, ss2, ms);
                        }
                        else
                        {
                            textField.text = string.Format(configuration.displayFormat, mm2, ss2, ms);
                        }
                        break;

                }
            }
            else
            {
                textField.text = text;
            }

            if (nameEntry)
            {
                inputField.text = configuration.customNameDefaultValue;
            }
        }
    }
}