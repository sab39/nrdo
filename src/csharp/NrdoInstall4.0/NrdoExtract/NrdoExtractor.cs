using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using NR.nrdo.Reflection;

namespace NR.nrdo.Extract
{
    public class NrdoExtractor : MarshalByRefObject
    {
        // Constructor doesn't do anything because this class is designed to be instantiated from a separate appdomain, and interaction will be done via a proxy
        public NrdoExtractor()
        {
        }

        List<Assembly> modules = new List<Assembly>();

        public void LoadDlls(string binFolder)
        {
            string[] dlls = Directory.GetFiles(binFolder);
            List<Assembly> modules = new List<Assembly>();
            foreach (string dll in dlls)
            {
                if (dll.EndsWith(".dll") && !dll.EndsWith("NR.nrdo.dll"))
                {
                    modules.Add(Assembly.LoadFrom(dll));
                }
            }
        }

        public void WriteFiles(string outputPath, string listFilePath)
        {
            var lookup = new Lookup(this);
            using (var projStream = new FileStream(listFilePath, FileMode.Create))
            {
                using (var projWriter = new StreamWriter(projStream))
                {
                    foreach (var table in NrdoTable.GetAllTables(lookup))
                    {
                        string dirPath = outputPath;
                        if (Nstring.Parse(table.Module) != null)
                        {
                            var tblModule = table.Module.Split(':');
                            for (int i = 0; i < tblModule.Length; i++)
                            {
                                tblModule[i] = char.ToUpper(tblModule[i][0]) + tblModule[i].Substring(1);
                            }
                            dirPath = Path.Combine(outputPath, Path.Combine(tblModule));
                            Directory.CreateDirectory(dirPath);
                        }
                        string fileName = Path.Combine(dirPath, table.UnqualifiedName + ".dfn");
                        using (var stream = new FileStream(fileName, FileMode.Create))
                        {
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(table.ToDfnSyntax());
                            }
                        }
                        projWriter.WriteLine("table " + table.Name);
                    }
                    foreach (var query in NrdoQuery.GetAllQueries(lookup))
                    {
                        // Originally only queries that had "storedproc" or "storedfunction" defined were
                        // included here. However, it turns out that even queries with no database representation
                        // are allowed to have Before statements present. So we need to include those too, just
                        // in case.
                        string dirPath = outputPath;
                        if (Nstring.Parse(query.Module) != null)
                        {
                            var qryModule = query.Module.Split(':');
                            for (int i = 0; i < qryModule.Length; i++)
                            {
                                qryModule[i] = char.ToUpper(qryModule[i][0]) + qryModule[i].Substring(1);
                            }
                            dirPath = Path.Combine(outputPath, Path.Combine(qryModule));
                            Directory.CreateDirectory(dirPath);
                        }
                        string fileName = Path.Combine(dirPath, query.UnqualifiedName + ".qu");
                        using (var stream = new FileStream(fileName, FileMode.Create))
                        {
                            using (var writer = new StreamWriter(stream))
                            {
                                writer.Write(query.ToDfnSyntax());
                            }
                        }
                        projWriter.WriteLine("query " + query.Name);
                    }
                }
            }
        }

        private class Lookup : ILookupAssemblies
        {
            private readonly NrdoExtractor extractor;
            internal Lookup(NrdoExtractor extractor)
            {
                this.extractor = extractor;
            }
            public IEnumerable<AssemblyName> GetAllKnownAssemblies()
            {
                foreach (var assembly in extractor.modules)
                {
                    yield return assembly.GetName();
                }
            }

            public IEnumerable<AssemblyName> GetPossibleAssemblies(string tableName)
            {
                foreach (var assembly in extractor.modules)
                {
                    yield return assembly.GetName();
                }
            }
        }
    }
}
