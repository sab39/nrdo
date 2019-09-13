using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NR.nrdo.Util
{
    public static class ReflectionUtil
    {
        public static IEnumerable<T> GetCustomAttributes<T>(this ICustomAttributeProvider provider)
            where T : Attribute
        {
            return provider.GetCustomAttributes(typeof(T), false).Cast<T>();
        }

        private static string getShortName(string fullName)
        {
            return fullName.Substring(0, fullName.IndexOf(','));
        }

        public static string GetShortName(this Assembly assembly)
        {
            return getShortName(assembly.FullName);
        }

        public static string GetShortName(this AssemblyName assemblyName)
        {
            return getShortName(assemblyName.FullName);
        }

        public static bool References(this Assembly assembly, string referencedShortName)
        {
            return assembly.GetReferencedAssemblies().Any(dep => dep.GetShortName() == referencedShortName);
        }
    }
}
