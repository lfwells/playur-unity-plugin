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
                var field = typeof(Parameters).GetField(kvp.Key);
                if (field!= null)
                { 
                    if (field.FieldType == typeof(string))
                    {
                        field.SetValue(null, kvp.Value);
                    }
                    else if (field.FieldType == typeof(bool))
                    {
                        bool b = false;
                        bool.TryParse(kvp.Value, out b);
                        field.SetValue(null, b);
                    }
                    else if (field.FieldType == typeof(float))
                    {
                        float f = 0.0f;
                        float.TryParse(kvp.Value, out f);
                        field.SetValue(null, f);
                    }
                    else if (field.FieldType == typeof(double))
                    {
                        double d = 0.0;
                        double.TryParse(kvp.Value, out d);
                        field.SetValue(null, d);
                    }
                    else if (field.FieldType == typeof(int))
                    {
                        int i = 0;
                        int.TryParse(kvp.Value, out i);
                        field.SetValue(null, i);
                    }
                    else
                    {
                        PlayURPlugin.LogWarning("Couldn't Auto-Populate Parameters Static Field:" + field?.Name + " to " + kvp.Value + " (unsupported field type "+field.FieldType+")");
                    }
                    
                    //PlayURPlugin.Log("Populating Parameters Static Field:" + field?.Name + " = " + kvp.Value);
                }
            }
        }
    }
}
