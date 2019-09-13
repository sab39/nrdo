using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace NR.nrdo.OldVersionLoader
{
    public class CacheFileReader : MarshalByRefObject
    {
        public CacheFileReader()
        {
        }

        private class FailureException : Exception
        {
            internal FailureException(string msg) : base(msg) { }
            internal FailureException(string msg, Exception inner) : base(msg, inner) { }
        }

        private Assembly loadAssembly(string fullPath, CacheResultCollectorBase collector, bool fatalErrors = false)
        {
            collector.StartedAssemblyLoad(fullPath);
            try
            {
                return Assembly.LoadFrom(fullPath);
            }
            catch (Exception ex)
            {
                if (fatalErrors) throw new FailureException("Failed to load " + fullPath, ex);

                collector.Warning("Failed to load " + fullPath + ": " + ex);
                return null;
            }
        }

        private Type getType(Assembly asm, string name)
        {
            try
            {
                var type = asm.GetType(name);
                if (type == null) throw new FailureException("No such type: " + name);
                return type;
            }
            catch (Exception ex)
            {
                throw new FailureException("Error getting type: " + name, ex);
            }
        }

        private Func<object, TResult> getProperty<TResult>(Type type, string name)
        {
            try
            {
                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance, null, typeof(TResult), Type.EmptyTypes, null);
                if (prop == null) throw new FailureException("No such property: " + type.Name + "." + name);
                return obj => (TResult)prop.GetValue(obj, null);
            }
            catch (Exception ex)
            {
                throw new FailureException("Error getting property: " + type.Name + "." + name, ex);
            }
        }

        public void ReadCacheFiles(CacheResultCollectorBase collector)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                var nrdo = loadAssembly(Path.Combine(baseDir, "NR.nrdo.dll"), collector, true);

                var tablesAttrType = getType(nrdo, "NR.nrdo.Attributes.NrdoTablesAttribute");
                var queriesAttrType = getType(nrdo, "NR.nrdo.Attributes.NrdoQueriesAttribute");
                var getTableType = getProperty<Type>(tablesAttrType, "Type");
                var getQueryType = getProperty<Type>(queriesAttrType, "Type");

                var objectBaseAttrType = getType(nrdo, "NR.nrdo.Attributes.NrdoObjectTypeAttribute");
                var getCacheFileName = getProperty<string>(objectBaseAttrType, "CacheFileName");
                var getCacheFileContents = getProperty<string>(objectBaseAttrType, "CacheFileContents");
                var tableAttrType = getType(nrdo, "NR.nrdo.Attributes.NrdoTableAttribute");
                var queryAttrType = getType(nrdo, "NR.nrdo.Attributes.NrdoQueryAttribute");

                foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
                {
                    var asm = loadAssembly(file, collector);
                    if (asm == null) continue;

                    foreach (var attr in asm.GetCustomAttributes(tablesAttrType, false))
                    {
                        var tableType = getTableType(attr);
                        var tableAttr = tableType.GetCustomAttributes(tableAttrType, false).SingleOrDefault();
                        if (tableAttr == null)
                        {
                            collector.Warning("Did not find " + tableAttrType.Name + " on " + tableType.Name);
                        }
                        else
                        {
                            collector.FoundCacheFile(getCacheFileName(tableAttr), getCacheFileContents(tableAttr));
                        }
                    }
                    foreach (var attr in asm.GetCustomAttributes(queriesAttrType, false))
                    {
                        var queryType = getQueryType(attr);
                        var queryAttr = queryType.GetCustomAttributes(queryAttrType, false).SingleOrDefault();
                        if (queryAttr == null)
                        {
                            collector.Warning("Did not find " + queryAttrType.Name + " on " + queryType.Name);
                        }
                        else
                        {
                            collector.FoundCacheFile(getCacheFileName(queryAttr), getCacheFileContents(queryAttr));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is FailureException)
                {
                    var msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += ": " + ex.InnerException;
                    }
                    collector.DidNotFindCacheAttributes(msg);
                }
                else
                {
                    collector.DidNotFindCacheAttributes("Unknown error: " + ex);
                }
            }
        }
    }
}
