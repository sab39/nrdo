using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NR.nrdo.Schema.OldVersionUpgrade;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Smelt;
using System.Diagnostics;
using System.Xml;

namespace CacheExtractionTest
{
    class Program
    {
        private const string defaultPath = @"\\nr-web6\Central\CMS6\Work\NrdoChanges\www";

        private static string timePrefix(TimeSpan timeSpan)
        {
            // All the standard timespan formats are overly verbose here. Our times will almost never exceed a minute or two, so using lots of digits for
            // hours/minutes/seconds is unnecessary, and sub-microsecond precision is silly. So we just give elapsed time in total seconds and assume that
            // three digits will probably be enough; if not then the string just gets longer.
            return string.Format("{1,7:###.000}s {0:HH:mm:ss} ", DateTime.Now, timeSpan.TotalSeconds);
        }

        private static string getConnectionStringFromWebConfig(string path)
        {
            var config = new XmlDocument();
            config.Load(Path.Combine(path, "web.config"));
            var connectionStringNode = (XmlElement)config.SelectSingleNode("configuration/connectionStrings/add[@name='ConnectionString']");
            return connectionStringNode.GetAttribute("connectionString");
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

            var connectionString = getConnectionStringFromWebConfig(sitePath);

            var cacheFolder = Path.Combine(Path.GetDirectoryName(sitePath), "nrdo-cache");
            var binFolder = Path.Combine(sitePath, "bin");

            OldVersionNrdoCache cache;
            if (Directory.Exists(cacheFolder))
            {
                Console.WriteLine("Using folder " + cacheFolder);
                cache = OldVersionNrdoCache.FromFolder(cacheFolder, output);
            }
            else if (File.Exists(Path.Combine(binFolder, "NR.nrdo.dll")))
            {
                Console.WriteLine("Using dlls from " + binFolder);
                cache = OldVersionNrdoCache.FromDlls(binFolder, output);
            }
            else
            {
                output.Error("Couldn't find anywhere to get cache files from");
                Console.Write("Press any key to continue: ");
                Console.ReadKey();
                return;
            }

            OldVersionUpgradeTool.UgpradeFromOldVersions(new SqlServerSchemaDriver(), connectionString, output, cache);

            Console.Write("Press any key to continue: ");
            Console.ReadKey();
        }
    }
}
