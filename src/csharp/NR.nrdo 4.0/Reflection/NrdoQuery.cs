using System;
using System.Collections.Generic;
using System.Text;
using NR.nrdo.Attributes;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public class NrdoQuery : NrdoObjectType
    {
        #region Internal static helper methods used in getting NrdoQuery objects - including constructor
        internal NrdoQuery(Type type) : base(type, type.GetAttribute<NrdoQueryAttribute>())
        {
            var queryAttr = type.GetAttribute<NrdoQueryAttribute>();
            this.sql = queryAttr.Sql;
            this.isStoredProc = queryAttr.StoredProc;
            this.isStoredFunction = queryAttr.StoredFunction;
            this.isPreUpgradeHook = queryAttr.PreUpgradeHook;

            MethodInfo call = type.GetMethod("Call");
            Type rtnType = call.ReturnType;
            if (rtnType.Equals(typeof(void)))
            {
                isVoid = true;
            }
            else if (rtnType.IsGenericType && rtnType.GetGenericTypeDefinition().Equals(typeof(List<>)))
            {
                isMulti = true;
            }
        }
        #endregion

        #region Public static methods for getting NrdoQuery objects

        public static NrdoQuery GetQuery(string name)
        {
            return NrdoReflection.GetCodeBase(Assembly.GetCallingAssembly()).GetQuery(name);
        }

        public static NrdoQuery GetQuery(Assembly assembly, string name)
        {
            return NrdoReflection.GetCodeBase(assembly).GetQuery(name);
        }

        public static NrdoQuery GetQuery(ILookupAssemblies lookup, string name)
        {
            return NrdoReflection.GetCodeBase(lookup).GetQuery(name);
        }

        public static NrdoQuery GetQuery(Type type)
        {
            Assembly assembly = type.Assembly;
            string name = NrdoCodeBase.getQueryNameFromType(type);
            var mangled = name == null ? null : NrdoReflection.MangleName(assembly, name);
            if (name == null || mangled != type.FullName) throw new ArgumentException(type.FullName + " is not a valid nrdo database query class (name = " + (name ?? "null") + ", mangled = " + (mangled ?? "null") + ")");
            NrdoQuery result = NrdoCodeBase.getQueryInternal(assembly, name, type);
            if (result == null) throw new ArgumentException(type.FullName + " is not a valid nrdo database query class");
            return result;
        }

        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the default lookup strategy of the calling
        /// assembly is used.
        /// </summary>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoQuery> GetAllQueries()
        {
            return NrdoReflection.GetCodeBase(Assembly.GetCallingAssembly()).AllQueries;
        }
        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the default lookup strategy of the given
        /// assembly is used.
        /// </summary>
        /// <param name="assembly">The assembly to use the default lookup strategy of. Note that the assembly's strategy might in theory NOT actually say to look in itself first, or at all.</param>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoQuery> GetAllQueries(Assembly assembly)
        {
            return NrdoReflection.GetCodeBase(assembly).AllQueries;
        }
        /// <summary>
        /// Gets all known tables. To determine which assemblies to load from, the given lookup strategy is used.
        /// </summary>
        /// <param name="lookup">The lookup strategy to use to find the appropriate assembly</param>
        /// <returns>NrdoTable objects corresponding to all known tables</returns>
        public static IEnumerable<NrdoQuery> GetAllQueries(ILookupAssemblies lookup)
        {
            return NrdoReflection.GetCodeBase(lookup).AllQueries;
        }
        #endregion

        #region General Information

        private string sql;
        public string Sql { get { return sql; } }

        private bool isStoredProc;
        public bool IsStoredProc { get { return isStoredProc; } }

        private bool isStoredFunction;
        public bool IsStoredFunction { get { return isStoredFunction; } }

        private bool isPreUpgradeHook;
        public bool IsPreUpgradeHook { get { return isPreUpgradeHook; } }

        private bool isMulti;
        public bool IsMulti { get { return isMulti; } }

        private bool isVoid;
        public bool IsVoid { get { return isVoid; } }

        #endregion

        #region Results
        private List<NrdoField> results;
        public IList<NrdoField> Results
        {
            get
            {
                resolveResults();
                return new ReadOnlyCollection<NrdoField>(results);
            }
        }

        public NrdoField GetResult(string name)
        {
            // FIXME: This can be made more efficient by storing a dictionary
            foreach (NrdoField field in Results)
            {
                if (field.Name == name) return field;
            }
            return null;
        }

        private void resolveResults()
        {
            if (results == null)
            {
                results = new List<NrdoField>();
                foreach (PropertyInfo prop in Type.GetProperties())
                {
                    var attr = prop.GetAttribute<NrdoFieldAttribute>();
                    if (attr != null)
                    {
                        results.Add(new NrdoField(this, attr, prop));
                    }
                }
                results.Sort();
            }
        }
        #endregion

        #region Params

        private List<NrdoParam> @params;
        public IList<NrdoParam> Params
        {
            get
            {
                resolveParams();
                return new ReadOnlyCollection<NrdoParam>(@params);
            }
        }

        private void resolveParams()
        {
            if (@params == null)
            {
                @params = new List<NrdoParam>();
                MethodInfo call = Type.GetMethod("Call");
                foreach (var attr in call.GetAttributes<NrdoParamAttribute>())
                {
                    @params.Add(new NrdoParam(attr));
                }
                @params.Sort();
            }
        }
        #endregion

        #region ToDfnSyntax method
        public string ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("query ");
            sb.Append(IsMulti ? "multi" : IsVoid ? "void" : "single");
            sb.Append(" " + Name + " {\r\n");
            if (IsStoredProc) sb.Append("  storedproc;\r\n");
            if (IsStoredFunction) sb.Append("  storedfunction;\r\n");
            if (IsPreUpgradeHook) sb.Append("  pre-upgrade-hook;\r\n");
            sb.Append(Params.DfnSyntaxBlock("  params", true));
            sb.Append(Results.DfnSyntaxBlock("  results", true));
            sb.Append("  sql [" + Sql.Replace("[", "[[").Replace("]", "[]") + "];\r\n");
            sb.Append(BeforeStatements.DfnSyntaxList(true));
            sb.Append("};");
            return sb.ToString();
        }
        #endregion
    }
}
