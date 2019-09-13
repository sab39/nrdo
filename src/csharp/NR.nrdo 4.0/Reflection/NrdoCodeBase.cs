using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using NR.nrdo.Attributes;
using System.Collections.ObjectModel;

namespace NR.nrdo.Reflection
{
    internal class NrdoCodeBase : INrdoCodeBase
    {
        private readonly ILookupAssemblies lookup;

        internal static NrdoCodeBase Get(ILookupAssemblies lookup)
        {
            return new NrdoCodeBase(lookup);
        }

        private NrdoCodeBase(ILookupAssemblies lookup)
        {
            this.lookup = lookup;
        }

        // FIXME all this static state predates the idea of a NrdoCodeBase object entirely
        // Should be changed to be stored on the instance, with locking appropriately
        // And make sure that NrdoCodeBase.Get() caches by lookup

        // These lookup tables are ThreadStatic to avoid locking and threadsafety issues. Since they are
        // read-only caches, and the information backing them never actually changes within any run of the
        // application, and there's no requirement that NrdoTable.Get() returns the same object for the same
        // table forever and ever amen, this is easier than trying to enforce that every accessor goes through
        // a lock.
        [ThreadStatic]
        private static Dictionary<Assembly, Dictionary<string, NrdoTable>> tablesByAssembly;
        private static Dictionary<Assembly, Dictionary<string, NrdoTable>> TablesByAssembly
        {
            get
            {
                if (tablesByAssembly == null) tablesByAssembly = new Dictionary<Assembly, Dictionary<string, NrdoTable>>();
                return tablesByAssembly;
            }
        }

        [ThreadStatic]
        private static Dictionary<ILookupAssemblies, Dictionary<string, NrdoTable>> tablesByLookup;
        private static Dictionary<ILookupAssemblies, Dictionary<string, NrdoTable>> TablesByLookup
        {
            get
            {
                if (tablesByLookup == null) tablesByLookup = new Dictionary<ILookupAssemblies, Dictionary<string, NrdoTable>>();
                return tablesByLookup;
            }
        }

        private static NrdoTable getTableInternal(Assembly assembly, string name)
        {
            return getTableInternal(assembly, name, null);
        }
        internal static NrdoTable getTableInternal(Assembly assembly, string name, Type type)
        {
            if (!TablesByAssembly.ContainsKey(assembly))
            {
                if (NrdoReflection.GetNsBase(assembly) == null) return null;
                TablesByAssembly[assembly] = new Dictionary<string, NrdoTable>();
            }
            Dictionary<string, NrdoTable> tables = TablesByAssembly[assembly];

            if (!tables.ContainsKey(name))
            {
                if (type == null) type = assembly.GetType(NrdoReflection.MangleName(assembly, name));
                tables[name] = (type == null ? null : new NrdoTable(type));
            }
            return tables[name];
        }
        private static IEnumerable<NrdoTable> getAllTablesInternal(Assembly assembly)
        {
            return from attr in assembly.GetAttributes<NrdoTablesAttribute>() select getTableInternal(assembly, getTableNameFromType(attr.Type), attr.Type);
        }
        private static void populateRenameMapping(Dictionary<string, string> renameMapping, Assembly assembly)
        {
            foreach (var attr in assembly.GetAttributes<NrdoTablesAttribute>())
            {
                renameMapping[attr.Name] = attr.Name;
                foreach (var oldName in attr.OldNames)
                {
                    if (!renameMapping.ContainsKey(oldName)) renameMapping[oldName] = attr.Name;
                }
            }
        }
        internal static string getTableNameFromType(Type type)
        {
            var tableAttr = type.GetAttribute<NrdoTableAttribute>();
            return tableAttr == null ? null : tableAttr.Name;
        }

        public IEnumerable<NrdoTable> AllTables
        {
            get
            {
                List<NrdoTable> tables = new List<NrdoTable>();
                Dictionary<Assembly, bool> assemblies = new Dictionary<Assembly, bool>();
                foreach (AssemblyName asmName in lookup.GetAllKnownAssemblies())
                {
                    Assembly assembly = Assembly.Load(asmName);

                    if (assemblies.ContainsKey(assembly)) continue; // Don't bother processing the same assembly twice
                    assemblies[assembly] = true;

                    var count = tables.Count;
                    tables.AddRange(getAllTablesInternal(assembly));
                }
                return new ReadOnlyCollection<NrdoTable>(tables);
            }
        }

        public NrdoTable GetTable(string name)
        {
            if (!TablesByLookup.ContainsKey(lookup)) TablesByLookup[lookup] = new Dictionary<string, NrdoTable>();
            Dictionary<string, NrdoTable> tables = TablesByLookup[lookup];

            if (!tables.ContainsKey(name))
            {
                NrdoTable table = null;
                foreach (AssemblyName asmName in lookup.GetPossibleAssemblies(name))
                {
                    Assembly assembly = Assembly.Load(asmName);
                    if (assembly == null) throw new ArgumentException("Failed to load assembly " + asmName.FullName + "; Codebase=" + asmName.CodeBase);
                    table = getTableInternal(assembly, name);
                    if (table != null) break;
                }
                tables[name] = table;
            }
            return tables[name];
        }

        public IDictionary<string, string> GetTableRenameMapping()
        {
            var renameMapping = new Dictionary<string, string>();
            Dictionary<Assembly, bool> assemblies = new Dictionary<Assembly, bool>();
            foreach (AssemblyName asmName in lookup.GetAllKnownAssemblies())
            {
                Assembly assembly = Assembly.Load(asmName);

                if (assemblies.ContainsKey(assembly)) continue; // Don't bother processing the same assembly twice
                assemblies[assembly] = true;

                populateRenameMapping(renameMapping, assembly);
            }
            return renameMapping;
        }


        private static Dictionary<Assembly, Dictionary<string, NrdoQuery>> queriesByAssembly = new Dictionary<Assembly, Dictionary<string, NrdoQuery>>();
        private static Dictionary<ILookupAssemblies, Dictionary<string, NrdoQuery>> queriesByLookup = new Dictionary<ILookupAssemblies, Dictionary<string, NrdoQuery>>();

        private static NrdoQuery getQueryInternal(Assembly assembly, string name)
        {
            return getQueryInternal(assembly, name, null);
        }
        internal static NrdoQuery getQueryInternal(Assembly assembly, string name, Type type)
        {
            if (!queriesByAssembly.ContainsKey(assembly))
            {
                if (NrdoReflection.GetNsBase(assembly) == null) return null;
                queriesByAssembly[assembly] = new Dictionary<string, NrdoQuery>();
            }
            Dictionary<string, NrdoQuery> queries = queriesByAssembly[assembly];

            if (!queries.ContainsKey(name))
            {
                if (type == null) type = assembly.GetType(NrdoReflection.MangleName(assembly, name));
                queries[name] = (type == null ? null : new NrdoQuery(type));
            }
            return queries[name];
        }
        private static IEnumerable<NrdoQuery> getAllQueriesInternal(Assembly assembly)
        {
            return from attr in assembly.GetAttributes<NrdoQueriesAttribute>() select NrdoQuery.GetQuery(attr.Type);
        }

        internal static string getQueryNameFromType(Type type)
        {
            var queryAttr = type.GetAttribute<NrdoQueryAttribute>();
            return queryAttr == null ? null : queryAttr.Name;
        }

        public IEnumerable<NrdoQuery> AllQueries
        {
            get
            {
                List<NrdoQuery> queries = new List<NrdoQuery>();
                Dictionary<Assembly, bool> assemblies = new Dictionary<Assembly, bool>();
                foreach (AssemblyName asmName in lookup.GetAllKnownAssemblies())
                {
                    Assembly assembly = Assembly.Load(asmName);

                    if (assemblies.ContainsKey(assembly)) continue; // Don't bother processing the same assembly twice
                    assemblies[assembly] = true;

                    queries.AddRange(getAllQueriesInternal(assembly));
                }
                return new ReadOnlyCollection<NrdoQuery>(queries);
            }
        }

        public NrdoQuery GetQuery(string name)
        {
            if (!queriesByLookup.ContainsKey(lookup)) queriesByLookup[lookup] = new Dictionary<string, NrdoQuery>();
            Dictionary<string, NrdoQuery> queries = queriesByLookup[lookup];

            if (!queries.ContainsKey(name))
            {
                NrdoQuery query = null;
                foreach (AssemblyName asmName in lookup.GetPossibleAssemblies(name))
                {
                    Assembly assembly = Assembly.Load(asmName);
                    if (assembly == null) throw new ArgumentException("Failed to load assembly " + asmName.FullName + "; Codebase=" + asmName.CodeBase);
                    query = getQueryInternal(assembly, name);
                    if (query != null) break;
                }
                queries[name] = query;
            }
            return queries[name];
        }
    }
}
