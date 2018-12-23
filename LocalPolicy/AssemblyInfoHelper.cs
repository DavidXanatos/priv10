using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LocalPolicy
{
    internal class AssemblyInfoHelper
    {
        internal static T GetAssemblyAttribute<T>()
            where T : Attribute
        {
            var assembly = Assembly.GetExecutingAssembly();
            return GetAssemblyAttribute<T>(assembly);            
        }

        internal static T GetAssemblyAttribute<T>(Assembly assembly)
            where T : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(T), true);
            if (attributes == null || attributes.Length == 0)
                return null;

            return (T)attributes.First();
        }
    }
}
