using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

namespace PlayUR
{
    public class ExperimentGroupDropdown : EnumDropdown<ExperimentGroup>
    {
        protected override void OnSelect(ExperimentGroup? option)
        {
            PlayURPlugin.Log($"Selected Group: {option}");
            if (option == null)
            {
                PlayURPlugin.instance.didRequestExperimentGroup = false;
            }
            else
            {
                PlayURPlugin.instance.didRequestExperimentGroup = true;
                PlayURPlugin.instance.requestedExperimentGroup = option.Value;
            }
        }
    }
}