using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.IO;
using NR.nrdo.Attributes;
using NR.nrdo.Schema.Tool;
using System.Reflection;
using System.Security.Policy;
using NR.nrdo.OldVersionLoader;
using System.Threading;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.OldVersionUpgrade
{
    internal static class OldVersionDllHelpers
    {
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
    }
    public sealed class OldVersionNrdoCache
    {
        private static bool eq(string a, string b)
        {
            // Writing this out in full every time obfuscates the code
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
        private static bool startsWith(string a, string b)
        {
            return a.StartsWith(b, StringComparison.OrdinalIgnoreCase);
        }

        public static OldVersionNrdoCache FromFolder(string path, IOutput output)
        {
            var stateFile = Path.Combine(path, "_state");
            return new OldVersionNrdoCache("Dir: " + path, File.Exists(stateFile) && eq(File.ReadAllText(stateFile).Trim(), "complete"),
                from file in Directory.GetFiles(path)
                let name = Path.GetFileName(file)
                where !eq(name, "_state") && !startsWith(name, ("_lock."))
                select new CacheEntry(name, File.ReadAllText(file)));
        }

        public static OldVersionNrdoCache FromDlls(string binFolder, IOutput output)
        {
            return new OldVersionNrdoCache("DLLs: " + binFolder, true, readBinFolder(binFolder, output));
        }

        public static IEnumerable<CacheEntry> readBinFolder(string binFolder, IOutput output)
        {
            var stubAssemblyPath = typeof(CacheFileReader).Assembly.Location;
            var stubDestPath = Path.Combine(binFolder, Path.GetFileName(stubAssemblyPath));

            try
            {
                // Tried various ways of doing this without actually putting the assembly into the bin folder temporarily, but none of them worked.
                if (File.Exists(stubDestPath)) throw new ApplicationException("Extraction code stub dll already exists at " + stubDestPath);
                File.Copy(stubAssemblyPath, Path.Combine(binFolder, stubDestPath));

                AppDomain domain = null;
                try
                {
                    domain = AppDomain.CreateDomain("OldVersion", AppDomain.CurrentDomain.Evidence, new AppDomainSetup
                    {
                        ApplicationBase = binFolder,
                        LoaderOptimization = LoaderOptimization.MultiDomainHost,
                    });

                    var reader = (CacheFileReader)domain.CreateInstanceAndUnwrap(typeof(CacheFileReader).Assembly.FullName, typeof(CacheFileReader).FullName);
                    var collector = new CacheReadResultCollector(output);
                    reader.ReadCacheFiles(collector);
                    if (collector.failureMessage != null) throw new ApplicationException("Failed to load cache: " + collector.failureMessage);
                    return collector.entries;
                }
                finally
                {
                    if (domain != null) AppDomain.Unload(domain);
                }
            }
            finally
            {
                var tries = 5;
                while (tries > 0 && File.Exists(stubDestPath))
                {
                    tries--;
                    File.Delete(stubDestPath);
                    Thread.Sleep(1000);
                }
                if (File.Exists(stubDestPath)) output.Warning("Could not delete " + stubDestPath);
            }
        }

        private sealed class CacheReadResultCollector : CacheResultCollectorBase
        {
            private readonly IOutput output;
            internal readonly List<CacheEntry> entries = new List<CacheEntry>();
            internal string failureMessage = null;

            internal CacheReadResultCollector(IOutput output)
            {
                this.output = output;
            }

            public override void DidNotFindCacheAttributes(string msg)
            {
                output.Error(msg);
                failureMessage = msg;
            }

            public override void Warning(string msg)
            {
                output.Warning(msg);
            }

            public override void StartedAssemblyLoad(string file)
            {
                output.Verbose("Loading assembly: " + file + "...");
            }

            public override void FoundCacheFile(string name, string contents)
            {
                entries.Add(new CacheEntry(name, contents));
            }
        }


        public sealed class CacheEntry
        {
            private readonly string name;
            public string Name { get { return name; } }

            private readonly string content;
            public string Content { get { return content; } }

            internal CacheEntry(string name, string content)
            {
                this.name = name;
                this.content = content;
            }
        }

        private OldVersionNrdoCache(string description, bool isComplete, IEnumerable<CacheEntry> entries)
        {
            this.description = description;
            this.isComplete = isComplete;
            this.entries = new Lazy<ImmutableList<CacheEntry>>(() => entries.ToImmutableList());
        }

        private readonly string description;
        public string Description { get { return description; } }

        private readonly bool isComplete;
        public bool IsComplete { get { return isComplete; } }

        private readonly Lazy<ImmutableList<CacheEntry>> entries;
        public ImmutableList<CacheEntry> Entries { get { return entries.Value; } }

        public override string ToString()
        {
            return description;
        }
    }
}
