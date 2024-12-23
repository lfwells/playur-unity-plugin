using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PlayUR/Detached Mode/Configuration Object")]
public class PlayURConfigurationObject : ScriptableObject
{
    [System.Serializable]
    public class Parameter
    {
        public enum DataType
        {
            String,
            Int,
            Float,
            Boolean
        }

        public string key;
        public string value;
        public DataType type;
    }

    public List<Parameter> parameters;
}
