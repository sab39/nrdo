using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NR.nrdo.Attributes;
using System.IO;
using System.Collections.Immutable;

namespace NR.nrdo.Reflection
{
    public static class NrdoReflection
    {
        public static INrdoCodeBase GetCodeBase()
        {
            return GetCodeBase(Assembly.GetCallingAssembly());
        }
        public static INrdoCodeBase GetCodeBase(Assembly assembly)
        {
            return GetCodeBase(NrdoReflection.GetLookupStrategy(assembly));
        }
        public static INrdoCodeBase GetCodeBase(ILookupAssemblies lookup)
        {
            return NrdoCodeBase.Get(lookup);
        }

        public static ILookupAssemblies GetLookupStrategy(Assembly assembly)
        {
            var lookupAttr = assembly.GetAttribute<NrdoAssemblyLookupAttribute>();
            if (lookupAttr != null)
            {
                Type lookupType = lookupAttr.Type;
                var ctor = lookupType.GetConstructor(new Type[] { typeof(Assembly) });
                if (ctor != null)
                {
                    return (ILookupAssemblies)ctor.Invoke(new object[] { assembly });
                }
                else
                {
                    return (ILookupAssemblies)Activator.CreateInstance(lookupType);
                }
            }
            else
            {
                return new ReferencedLookupAssemblies(assembly);
            }
        }

        /// <summary>
        /// Write nrdo-cache files for every table in an assembly to a folder. This function is called externally from
        /// code that is potentially from a different version of nrdo (a newer version calls an older version) so it
        /// is vital that the signature and behavior of this method remain consistent over time.
        /// </summary>
        /// <param name="assembly">The assembly to process</param>
        /// <param name="folder">The folder to write cache files to</param>
        public static void WriteNrdoCacheFiles(Assembly assembly, string folder)
        {
            File.WriteAllText(Path.Combine(folder, "_state"), "complete", Encoding.ASCII);
            foreach (var attr in assembly.GetAttributes<NrdoTablesAttribute>())
            {
                var table = attr.Type.GetAttribute<NrdoTableAttribute>();
                File.WriteAllText(Path.Combine(folder, table.CacheFileName), table.CacheFileContents, Encoding.ASCII);
            }
            foreach (var attr in assembly.GetAttributes<NrdoQueriesAttribute>())
            {
                var query = attr.Type.GetAttribute<NrdoQueryAttribute>();
                File.WriteAllText(Path.Combine(folder, query.CacheFileName), query.CacheFileContents, Encoding.ASCII);
            }
        }

        internal static List<T> GetAttributes<T>(this ICustomAttributeProvider obj)
            where T : Attribute
        {
            return obj.GetCustomAttributes(typeof(T), false).Cast<T>().ToList();
        }

        internal static T GetAttribute<T>(this ICustomAttributeProvider obj)
            where T : Attribute
        {
            return obj.GetAttributes<T>().SingleOrDefault();
        }

        internal static string MapAndJoin<T>(this IEnumerable<T> items, string separator, Func<T, string> mapping)
        {
            return string.Join(separator, items.Select(mapping));
        }

        private static ImmutableDictionary<Assembly, string> nsbases = ImmutableDictionary<Assembly, string>.Empty;
        internal static string GetNsBase(Assembly assembly)
        {
            if (nsbases.ContainsKey(assembly)) return nsbases[assembly];

            var attr = assembly.GetAttribute<NrdoNsBaseAttribute>();
            var result = (attr == null ? null : attr.NsBase);
            nsbases = nsbases.SetItem(assembly, result);
            return result;
        }

        internal static string MangleName(Assembly assembly, string name)
        {
            string result = GetNsBase(assembly);
            if (result == null) return null;
            foreach (string part in name.Split(':'))
            {
                result += ".";
                foreach (string word in part.Split('_'))
                {
                    if (word != "")
                    {
                        result += char.ToUpper(word[0]) + word.Substring(1);
                    }
                }
            }
            return result;
        }

        internal static string DfnSyntaxList<T>(this IEnumerable<T> items) where T : IDfnElement
        {
            return items.MapAndJoin("; ", t => t.ToDfnSyntax());
        }
        internal static string DfnSyntaxList<T>(this IEnumerable<T> items, bool newlines) where T : IDfnElement
        {
            return newlines ? items.MapAndJoin("", t => t.ToDfnSyntax() + ";\r\n") : items.DfnSyntaxList();
        }

        internal static string DfnSyntaxBlock<T>(this IList<T> items, string prefix, bool newlines) where T : IDfnElement
        {
            StringBuilder sb = new StringBuilder();
            if (items.Count > 0)
            {
                sb.Append(prefix);
                sb.Append(" {");
                if (newlines) sb.Append("\r\n");
                sb.Append(DfnSyntaxList(items, newlines));
                if (newlines)
                {
                    int i = 0;
                    while (prefix[i++] == ' ') sb.Append(' ');
                }
                sb.Append("};\r\n");
            }
            return sb.ToString();
        }

        internal static string GetTypeString(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(char)) return "char";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(DateTime)) return "DateTime";
            return type.FullName;
        }    
    }
}
