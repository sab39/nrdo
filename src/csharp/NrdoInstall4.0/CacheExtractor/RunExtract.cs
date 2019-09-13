using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace NR.nrdo.Install
{
    static class RunExtract
    {
        private static void error(string message)
        {
            Progress.Fail(message + "\r\nExtraction failed.");
        }
        internal static TDelegate StaticMethod<TDelegate>(this Type type, string name)
        {
            var delegateMethod = typeof(TDelegate).GetMethod("Invoke");
            var parameterTypes = delegateMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            var staticMethod = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public, null, parameterTypes, null);
            return (TDelegate)(object)Delegate.CreateDelegate(typeof(TDelegate), staticMethod);
        }
        public static void Run(string binBase, string cacheBase, string initialError)
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

                if (!Directory.Exists(cacheBase)) Directory.CreateDirectory(cacheBase);
                cacheBase = Path.GetFullPath(cacheBase);

                // The hardcoded exclusion of CuteWebUI.AjaxUploader here is ugly, but the Assembly.LoadFrom
                // fails on it and I can't figure out a way to distinguish that "legitimate" failure from
                // a failure that we should report. All the information that would tell us whether a particular
                // dll is supposed to link to nrdo is only available once the Load succeeds.
                var dlls = (from dll in Directory.GetFiles(binBase, "*.dll")
                            where !dll.EndsWith("NR.nrdo.dll") && !dll.EndsWith("CuteWebUI.AjaxUploader.dll")
                            select dll).ToList();

                Progress.Total = dlls.Count * 2 + 1;

                var nrdoAsm = Assembly.LoadFrom(Path.Combine(binBase, "NR.nrdo.dll"));
                var reflectionFunctions = nrdoAsm.GetType("NR.nrdo.Reflection.NrdoReflection", true);
                var writeCacheFiles = reflectionFunctions.StaticMethod<Action<Assembly, string>>("WriteNrdoCacheFiles");
                Progress.Current++;

                foreach (string dll in dlls)
                {
                    Progress.Report("Loading " + dll);
                    var asm = Assembly.LoadFrom(dll);
                    Progress.Current++;
                    Progress.Report("Writing cache files for " + dll);
                    writeCacheFiles(asm, cacheBase);
                    Progress.Current++;
                }
                Progress.Done("Extraction successful.");
            }
            catch (Exception e)
            {
                error(e.GetType().Name + ": " + e.Message + "\r\n" + e.StackTrace);
            }
        }
    }
}
