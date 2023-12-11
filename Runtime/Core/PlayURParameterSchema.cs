using Codice.CM.Client.Differences.Merge;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PlayUR.ParameterSchemas
{
    public class EntityData
    {
        //the base class has a ToJson function, and all subclasses have a ToJson, but it is defined using extension methods
        //this is a hack to get the base class to call the extension method on the subclass
        public string ToJson() 
        {
            var extendedType = this.GetType();
            Assembly assembly = extendedType.Assembly;
            var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
            var query = from type in assembly.GetTypes()
                        where type.IsSealed && !type.IsGenericType && !type.IsNested
                        from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsDefined(typeof(ExtensionAttribute), false)
                        where isGenericTypeDefinition
                            ? method.GetParameters()[0].ParameterType.IsGenericType && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == extendedType
                            : method.GetParameters()[0].ParameterType == extendedType
                        select method;

            if (query.Count() == 1)
            {
                return query.Single().Invoke(this, new object[] {this}).ToString();
            }

            return string.Empty;  
        }

        public static T FromJson<T>(string json) where T : EntityData
        {
            var extendedType = typeof(T);
            Assembly assembly = extendedType.Assembly;
            var isGenericTypeDefinition = extendedType.IsGenericType && extendedType.IsTypeDefinition;
            var query = from method in extendedType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        where method.IsStatic
                        where method.Name == "FromJson"
                        select method;
            if (query.Count() == 1)
            {
                return query.Single().Invoke(null, new object[] { json }) as T;
            }

            return default;
        }
    }
}