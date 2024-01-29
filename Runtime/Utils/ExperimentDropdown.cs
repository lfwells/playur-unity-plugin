using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

namespace PlayUR
{
    public class ExperimentDropdown : EnumDropdown<Experiment>
    {
    }


    public class EnumDropdown<T> : MonoBehaviour where T : struct, Enum
    {
        private void Start()
        {
            var dropdown = GetComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(Enum.GetValues(typeof(T)).Cast<T>().Select(e => new Dropdown.OptionData(CamelCaseToSpaced(e.ToString()))).ToList());
        }

        //convert camel case to have spaces
        private string CamelCaseToSpaced(string s)
        {
            return System.Text.RegularExpressions.Regex.Replace(s, "(\\B[A-Z])", " $1");
        }

    }
}