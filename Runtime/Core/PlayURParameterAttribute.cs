using System;
using System.Reflection;

namespace PlayUR
{
    public class PlayURParameterAttribute : Attribute
    {
        public string key;
        public object defaultValue;
        public bool warn = true;

        public PlayURParameterAttribute(string key)
        {
            this.key = key;
        }

        public void Apply(FieldInfo field, object obj)
        {
            if (field.FieldType == typeof(string))
            {
                ApplyString(field, obj);
            }
            else if (field.FieldType == typeof(int))
            {
                ApplyInt(field, obj);
            }
            else if (field.FieldType == typeof(float))
            {
                ApplyFloat(field, obj);
            }
            else if (field.FieldType == typeof(bool))
            {
                ApplyBool(field, obj);
            }
            else
            {
                PlayURPlugin.LogError($"Unsupported parameter type {field.FieldType.FullName} on field {field.Name} ({field.ReflectedType.FullName})", (UnityEngine.Object)obj);
            }
        }
        void ApplyString(FieldInfo field, object obj)
        { 
            if (string.IsNullOrEmpty((string)defaultValue))
            {
                defaultValue = (string)field.GetValue(obj);
            }
            var value = PlayURPlugin.instance.GetStringParam(key, defaultValue: (string)defaultValue, warn: warn);
            field.SetValue(obj, value);
        }

        void ApplyInt(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetIntParam(key, defaultValue: (int)defaultValue, warn: warn);
            field.SetValue(obj, value);
        }

        void ApplyFloat(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetFloatParam(key, defaultValue: (float)defaultValue, warn: warn);
            field.SetValue(obj, value);
        }

        void ApplyBool(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetBoolParam(key, defaultValue: (bool)defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
    }

}