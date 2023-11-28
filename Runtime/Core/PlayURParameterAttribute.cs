using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PlayUR
{
    //TODO: documentation
    //TODO: inspector for this
    public class PlayURParameterAttribute : PropertyAttribute
    {
        public string key;
        public object defaultValue;
        public bool warn = true;
        public bool crash = false;

        public PlayURParameterAttribute(string key)
        {
            this.key = key;
        }

        public void Apply(FieldInfo field, object obj)
        {
            if (crash && PlayURPlugin.instance.ParamExists(key) == false) 
            {
                throw new PlayUR.Exceptions.ParameterNotFoundException(key);
            }

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
            else if (field.FieldType == typeof(string[]))
            {
                ApplyStringArray(field, obj);
            }
            else if (field.FieldType == typeof(int[]))
            {
                ApplyIntArray(field, obj);
            }
            else if (field.FieldType == typeof(float[]))
            {
                ApplyFloatArray(field, obj);
            }
            else if (field.FieldType == typeof(bool[]))
            {
                ApplyBoolArray(field, obj);
            }
            else if (field.FieldType == typeof(List<string>))
            {
                ApplyStringList(field, obj);
            }
            else if (field.FieldType == typeof(List<int>))
            {
                ApplyIntList(field, obj);
            }
            else if (field.FieldType == typeof(List<float>))
            {
                ApplyFloatList(field, obj);
            }
            else if (field.FieldType == typeof(List<bool>))
            {
                ApplyBoolList(field, obj);
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

        void ApplyStringArray(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetStringParamList(key, defaultValue: (string[])defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
        void ApplyStringList(FieldInfo field, object obj)
        {
            var d = defaultValue == null ? null : ((string[])defaultValue).ToArray();
            var value = PlayURPlugin.instance.GetStringParamList(key, defaultValue: d, warn: warn)?.ToList();
            field.SetValue(obj, value);
        }

        void ApplyIntArray(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetIntParamList(key, defaultValue: (int[])defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
        void ApplyIntList(FieldInfo field, object obj)
        {
            var d = defaultValue == null ? null : ((int[])defaultValue).ToArray();
            var value = PlayURPlugin.instance.GetIntParamList(key, defaultValue: d, warn: warn)?.ToList();
            field.SetValue(obj, value);
        }

        void ApplyFloatArray(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetFloatParamList(key, defaultValue: (float[])defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
        void ApplyFloatList(FieldInfo field, object obj)
        {
            var d = defaultValue == null ? null : ((float[])defaultValue).ToArray();
            var value = PlayURPlugin.instance.GetFloatParamList(key, defaultValue: d, warn: warn)?.ToList();
            field.SetValue(obj, value);
        }

        void ApplyBoolArray(FieldInfo field, object obj)
        {
            var value = PlayURPlugin.instance.GetBoolParamList(key, defaultValue: (bool[])defaultValue, warn: warn);
            field.SetValue(obj, value);
        }
        void ApplyBoolList(FieldInfo field, object obj)
        {
            var d = defaultValue == null ? null : ((bool[])defaultValue).ToArray();
            var value = PlayURPlugin.instance.GetBoolParamList(key, defaultValue: d, warn: warn)?.ToList();
            field.SetValue(obj, value);
        }
    }

}