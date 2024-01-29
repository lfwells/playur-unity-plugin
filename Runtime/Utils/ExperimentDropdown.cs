using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace PlayUR
{
    public class ExperimentDropdown : EnumDropdown<Experiment>
    {
        protected override void OnSelect(Experiment? option)
        {
            PlayURPlugin.Log($"Selected Experiment: {option}");
            if (option == null)
            {
                PlayURPlugin.instance.didRequestExperiment = false;
            }
            else
            {
                PlayURPlugin.instance.didRequestExperiment = true;
                PlayURPlugin.instance.requestedExperiment = option.Value;
            }
        }
    }


    public abstract class EnumDropdown<T> : MonoBehaviour where T : struct, Enum
    {
        public string blankOption = "-- Not Specified --";
        readonly List<T?> list = new List<T?>();

        private void Start()
        {
            list.Add(null);//blank option
            list.AddRange(Enum.GetValues(typeof(T)).Cast<T?>());

            var dropdown = GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(list.Select(e => e == null ? new Dropdown.OptionData(blankOption) : new Dropdown.OptionData(CamelCaseToSpaced(e.ToString()))).ToList());
            dropdown.onValueChanged.AddListener(OnSelectEvent);
        }

        //convert camel case to have spaces
        private string CamelCaseToSpaced(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, "(\\B[A-Z])", " $1");
        }

        void OnSelectEvent(int i)
        {
            OnSelect(list[i]);
        }

        protected abstract void OnSelect(T? option);

    }
}