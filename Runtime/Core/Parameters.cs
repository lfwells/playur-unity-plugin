using System.Reflection;

namespace PlayUR
{
    public static partial class Parameters
    {
        /// <summary>
        /// Populates the values in the generated file. Can only be called if plugin is ready. 
        /// Is automatically called by plugin when configuration is ready.
        /// </summary>
        /// <throws>ConfigurationNotReadyException</throws>
        public static void Load()
        {
            if (PlayURPlugin.IsReady == false)
            {
                throw new Exceptions.ConfigurationNotReadyException();
            }

            foreach (var kvp in PlayURPlugin.instance.Configuration.parameters)
            {
                //find the field for this key via reflection
                var field = typeof(Parameters).GetField(kvp.Key, BindingFlags.Static);
                if (field!= null)
                { 
                    Debug.Log(field?.Name);
                    if (field != null)
                    {
                        field.SetValue(null, kvp.Value);
                    }
                }
            }
        }
    }
}
