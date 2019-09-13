using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using NR.nrdo.Reflection;
using System.Diagnostics;
using net.netreach.nrdo.tools;
using System.Xml;
using NR.nrdo.Util;
using net.netreach.util;
using System.Windows.Forms;

namespace NR.nrdo.Install
{
    public static class RunInstall
    {
        public static void Run(string connStr, string binBase, string cacheBase, string initialError)
        {
            if (initialError != null)
            {
                Progress.Fail(initialError);
                return;
            }
            try
            {
                if (!Directory.Exists(binBase))
                {
                    error("BinBase directory " + binBase + " does not exist.");
                    return;
                }
                binBase = Path.GetFullPath(binBase);

                if (!Directory.Exists(cacheBase))
                {
                    error("CacheBase directory " + cacheBase + " does not exist.");
                    return;
                }
                cacheBase = Path.GetFullPath(cacheBase);

                string[] dlls = Directory.GetFiles(binBase);
                Progress.Total = dlls.Length * 10;
                List<Assembly> modules = new List<Assembly>();
                foreach (string dll in dlls)
                {
                    if (dll.EndsWith(".dll") && !dll.EndsWith("NR.nrdo.dll"))
                    {
                        Progress.Report("Loading " + dll);
                        modules.Add(Assembly.LoadFrom(dll));
                    }
                    Progress.Current++;
                }
                Progress.Report("Scanning...");
                Lookup lookup = new Lookup(modules);
                List<NrdoTable> tables = new List<NrdoTable>(NrdoTable.GetAllTables(lookup));
                List<NrdoQuery> queries = new List<NrdoQuery>(NrdoQuery.GetAllQueries(lookup));

                Progress.Total = dlls.Length + (tables.Count + queries.Count) * 2;
                Progress.Current++;
                if (Directory.Exists("_install\\dfns")) Directory.Delete("_install\\dfns", true);
                Directory.CreateDirectory("_install\\dfns");
                using (FileStream projStream = new FileStream("_install\\dfns\\install.nrdoproj", FileMode.Create))
                {
                    using (StreamWriter projWriter = new StreamWriter(projStream))
                    {
                        foreach (NrdoTable table in tables)
                        {
                            string dir;
                            if (Nstring.Parse(table.Module) != null)
                            {
                                string[] tblModule = table.Module.Split(':');
                                for (int i = 0; i < tblModule.Length; i++)
                                {
                                    tblModule[i] = char.ToUpper(tblModule[i][0]) + tblModule[i].Substring(1);
                                }
                                dir = string.Join("\\", tblModule);
                                Directory.CreateDirectory("_install\\dfns\\" + dir);
                                dir += "\\";
                            }
                            else
                            {
                                dir = "";
                            }
                            string fileName = dir + table.UnqualifiedName + ".dfn";
                            Progress.Report("Writing " + fileName);
                            using (FileStream stream = new FileStream("_install\\dfns\\" + fileName, FileMode.Create))
                            {
                                using (StreamWriter writer = new StreamWriter(stream))
                                {
                                    writer.Write(table.ToDfnSyntax());
                                }
                            }
                            projWriter.WriteLine("table " + table.Name);
                            Progress.Current++;
                        }
                        foreach (NrdoQuery query in queries)
                        {
                            // Originally only queries that had "storedproc" or "storedfunction" defined were
                            // included here. However, it turns out that even queries with no database representation
                            // are allowed to have Before statements present. So we need to include those too, just
                            // in case.
                            string dir;
                            if (Nstring.Parse(query.Module) != null)
                            {
                                string[] qryModule = query.Module.Split(':');
                                for (int i = 0; i < qryModule.Length; i++)
                                {
                                    qryModule[i] = char.ToUpper(qryModule[i][0]) + qryModule[i].Substring(1);
                                }
                                dir = string.Join("\\", qryModule);
                                Directory.CreateDirectory("_install\\dfns\\" + dir);
                                dir += "\\";
                            }
                            else
                            {
                                dir = "";
                            }
                            string fileName = dir + query.UnqualifiedName + ".qu";
                            Progress.Report("Writing " + fileName);
                            using (FileStream stream = new FileStream("_install\\dfns\\" + fileName, FileMode.Create))
                            {
                                using (StreamWriter writer = new StreamWriter(stream))
                                {
                                    writer.Write(query.ToDfnSyntax());
                                }
                            }
                            projWriter.WriteLine("query " + query.Name);
                            Progress.Current++;
                        }
                    }
                }

                Progress.Report("Writing install.nrdo");
                using (FileStream stream = new FileStream("_install\\install.nrdo", FileMode.Create))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(@"config {
  cachebase [" + cacheBase + @"];
  dburl [jdbc:sharp:System.Data.SqlClient:" + connStr + @";];
  dbadapter mssqlserver;
  dbdriver [JdbcSharp.SharpDriver, JdbcSharp];
  forcedrops;
  schema dbo;
  dfnbase dfns\\install.nrdoproj;
  strict orderby;
  strict deps;
  srcbase .;
  cgltemplate install.nrdo;
  querytemplate install.nrdo;
};");
                    }
                }
                Progress.Current++;

                Progress.Report("Running nrdo table creator...");
                var output = new ProgressOutputProvider(Progress.Current, Progress.Total);
                Output.addOutputProvider(output, true);
                Output.setPromptProvider(output);
                try
                {
                    Config config = Config.get("_install\\install.nrdo");
                    new TableCreator().doMain(config, true, null);
                    Progress.Current = Progress.Total;
                    Directory.Delete("_install", true);
                    Progress.Done("Installation successful.");
                }
                catch (Exception e)
                {
                    if (!(e is TableCreator.AbortException))
                    {
                        Output.reportException(e);
                        Progress.Fail("Installation failed.", e);
                    }
                    else
                    {
                        Progress.Fail("Installation cancelled.");
                    }
                }
            }
            catch (Exception e)
            {
                Progress.Fail("Installation failed.", e);
            }
        }

        private static void error()
        {
            Progress.Fail("Installation failed.");
        }
        private static void error(string message)
        {
            Progress.Fail(message + "\r\nInstallation failed.");
        }
    }
    class Lookup : ILookupAssemblies
    {
        private List<Assembly> assemblies;
        internal Lookup(List<Assembly> assemblies)
        {
            this.assemblies = assemblies;
        }
        public IEnumerable<AssemblyName> GetAllKnownAssemblies()
        {
            foreach (Assembly assembly in assemblies)
            {
                yield return assembly.GetName();
            }
        }

        public IEnumerable<AssemblyName> GetPossibleAssemblies(string tableName)
        {
            foreach (Assembly assembly in assemblies)
            {
                yield return assembly.GetName();
            }
        }
    }
    class ProgressOutputProvider : OutputProvider, PromptProvider
    {
        private int initial;
        private int final;
        private int total;
        private int current;

        internal ProgressOutputProvider(int initial, int final)
        {
            this.initial = initial;
            this.final = final;
        }

        public void reportError(FileLocation loc, string str)
        {
            if (loc != null)
            {
                Progress.Report("Error: " + loc.toString() + ": " + str);
            }
            else
            {
                Progress.Report("Error: " + str);
            }
        }

        public void println(string str)
        {
            Progress.Report(str);
        }

        public bool prompt(string prompt, string question)
        {
            return MessageBox.Show(prompt + "\r\n\r\n" + question, "Installation", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        public void setCurrentProgress(int i)
        {
            current = i;
            refreshProgress();
        }

        public void setTotalProgress(int i)
        {
            total = i;
            refreshProgress();
        }

        private void refreshProgress()
        {
            Progress.Current = initial + ((final - initial) * current) / total;
        }
    }
}
