using UnityEngine;

namespace PlayUR.Samples
{
    /// <summary>
    /// Sample MonoBehaviour which spawns a number of prefabs equal to the value of the parameter defined by <see cref="parameterKey"/>.
    /// Shows a <see cref="defaultString"/> until the plugin is Ready (<see cref="PlayURPlugin.Ready"/>). 
    /// Doesn't handle <see cref="ParameterNotFoundException"/>.
    /// </summary>
    public class SampleParameterInt : PlayURBehaviour
    {
        /// <summary>
        /// The key of the parameter defined on the back-end for the parameter.
        /// </summary>
        public string parameterKey = "testKeyNumber";

        /// <summary>
        /// The prefab to spawn repeatedly.
        /// </summary>
        public GameObject prefabToSpawn;

        /// <summary>
        /// The offset to add between each spawned prefab
        /// </summary>
        public Vector2 offset;

        public override void OnReady()
        {
            int count = PlayURPlugin.GetIntParam(parameterKey);
            Vector2 overallOffset = count/2f * offset + offset/2; 
            for (var i = 0; i < count; i++)
            {
                Instantiate(prefabToSpawn, (Vector2)transform.position + offset * i - overallOffset, Quaternion.identity); 
            }
        }
    }
}