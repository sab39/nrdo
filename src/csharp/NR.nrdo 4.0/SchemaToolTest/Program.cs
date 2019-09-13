using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Reflection;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using NR.nrdo.Schema.Providers;
using NR.nrdo.Util.OutputUtil;

namespace SchemaToolTest
{
    class Program
    {
        private const string defaultPath = @"C:\sballard\CMS6\Work\NrdoChanges";

        private static string timePrefix(TimeSpan timeSpan)
        {
            // All the standard timespan formats are overly verbose here. Our times will almost never exceed a minute or two, so using lots of digits for
            // hours/minutes/seconds is unnecessary, and sub-microsecond precision is silly. So we just give elapsed time in total seconds and assume that
            // three digits will probably be enough; if not then the string just gets longer.
            return string.Format("{1,7:###.000}s {0:HH:mm:ss} ", DateTime.Now, timeSpan.TotalSeconds);
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Default path: " + defaultPath);
            Console.Write("Enter path to site: ");
            var sitePath = Console.ReadLine();

            var stopwatch = Stopwatch.StartNew();
            var output = Output.Console(() => timePrefix(stopwatch.Elapsed));

            if (string.IsNullOrWhiteSpace(sitePath)) sitePath = defaultPath;

            var webConfig = Path.Combine(sitePath, "web.config");
            if (!File.Exists(webConfig))
            {
                output.Error("File not found: " + webConfig);
                Console.Write("Press any key to continue: ");
                Console.ReadKey();
                return;
            }

            var schemaDriver = new SqlServerSchemaDriver();

            var lookup = new LoadFromDllFolderLookupAssemblies(sitePath, output);
            var codeBase = NrdoReflection.GetCodeBase(lookup);
            var codeBaseProvider = new CodeBaseSchemaProvider(codeBase);

            var connectionString = getConnectionStringFromWebConfig(sitePath);

            SchemaTool.UpdateSchema(schemaDriver, connectionString, output, codeBaseProvider);
            stopwatch.Stop();

            Console.Write("Press any key to continue: ");
            Console.ReadKey();
        }

        private static string getConnectionStringFromWebConfig(string path)
        {
            var config = new XmlDocument();
            config.Load(Path.Combine(path, "web.config"));
            var connectionStringNode = (XmlElement)config.SelectSingleNode("configuration/connectionStrings/add[@name='ConnectionString']");
            return connectionStringNode.GetAttribute("connectionString");
        }

        private class LoadFromDllFolderLookupAssemblies : DllFolderLookupAssemblies
        {
            private readonly IOutput output;

            internal LoadFromDllFolderLookupAssemblies(string path, IOutput output)
                : base(new DirectoryInfo(Path.Combine(path, "bin")))
            {
                this.output = output;
            }

            protected override AssemblyName LoadAssembly(string name)
            {
                try
                {
                    output.Verbose("Loading assembly " + dir + "\\" + name + ".dll");
                    return Assembly.LoadFrom(dir + "\\" + name + ".dll").GetName();
                }
                catch
                {
                    output.Warning("Could not load " + dir + "\\" + name + ".dll");
                    return null;
                }
            }
        }
    }
}
