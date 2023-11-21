using System;
using System.Reflection;

namespace PlayUR
{
    public class PlayURStringParameterAttribute : Attribute
    {
        public string key;
        public string defaultValue;
        public bool warn = false;

        public PlayURStringParameterAttribute(string key)
        {
            this.key = key;
        }

        public void Apply(FieldInfo field, object obj)
        {
            if (string.IsNullOrEmpty(defaultValue))
            {
                defaultValue = (string)field.GetValue(obj);
            }
            var value = PlayURPlugin.instance.GetStringParam(key, defaultValue: defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
    }

}