using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace NR.nrdo.Reflection
{
    public class DllFolderLookupAssemblies : ILookupAssemblies
    {
        protected readonly DirectoryInfo dir;
        protected readonly Dictionary<string, AssemblyName> assemblies = new Dictionary<string,AssemblyName>(StringComparer.OrdinalIgnoreCase);
        protected bool loadedAll = false;

        public DllFolderLookupAssemblies(DirectoryInfo dir)
        {
            this.dir = dir;
        }

        protected virtual AssemblyName LoadAssembly(string name)
        {
            return new AssemblyName(name);
        }

        private AssemblyName getAssemblyName(string name)
        {
            AssemblyName result;
            if (assemblies.TryGetValue(name, out result)) return result;

            if (!loadedAll)
            {
                result = LoadAssembly(name);
                assemblies[name] = result;
            }

            return result;
        }

        public IEnumerable<AssemblyName> GetAllKnownAssemblies()
        {
            lock (this)
            {
                if (!loadedAll)
                {
                    foreach (FileInfo file in dir.GetFiles())
                    {
                        string lname = file.Name.ToLowerInvariant();
                        if (lname.EndsWith(".dll") && !lname.EndsWith(".nrdo.dll"))
                        {
                            getAssemblyName(file.Name.Substring(0, file.Name.LastIndexOf('.')));
                        }
                    }
                    loadedAll = true;
                }
            }
            return from assembly in assemblies.Values
                   where assembly != null
                   select assembly;
        }

        public IEnumerable<AssemblyName> GetPossibleAssemblies(string tableName)
        {
            lock (this)
            {
                string moduleName = tableName.Substring(0, tableName.IndexOf(':'));
                string asmName = char.ToUpper(moduleName[0]) + moduleName.Substring(1);
                yield return getAssemblyName(asmName);
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as DllFolderLookupAssemblies;
            return other != null && object.Equals(dir, other.dir);
        }

        public override int GetHashCode()
        {
            return dir.GetHashCode();
        }
    }
}
