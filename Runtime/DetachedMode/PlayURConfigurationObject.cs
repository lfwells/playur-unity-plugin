using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayUR.DetachedMode
{
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
            [TextArea(2, 10)]
            public string value;
            public DataType type;
        }

        public List<Parameter> parameters;
    }
}