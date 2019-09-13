using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NR.nrdo.Reflection
{
    public class ReferencedLookupAssemblies : ILookupAssemblies
    {
        private Assembly assembly;
        private string codeBase;
        private string shortName;
        public ReferencedLookupAssemblies(Assembly assembly)
        {
            this.assembly = assembly;
            codeBase = assembly.GetName().CodeBase;
            shortName = getShortName(assembly.GetName());
        }

        public IEnumerable<AssemblyName> GetPossibleAssemblies(string tableName)
        {
            return GetAllKnownAssemblies();
        }
        
        private static AssemblyName thisAssemblyName = null;
        public IEnumerable<AssemblyName> GetAllKnownAssemblies()
        {
            if (thisAssemblyName == null) thisAssemblyName = Assembly.GetExecutingAssembly().GetName();

            List<AssemblyName> list = new List<AssemblyName>(new AssemblyName[] { assembly.GetName() });
            while (list.Count > 0)
            {
                foreach (AssemblyName asmName in list)
                {
                    yield return asmName;
                }
                list = new List<AssemblyName>(getReferencedAssembliesOf(list));
            }
        }
        private IEnumerable<AssemblyName> getReferencedAssembliesOf(Assembly assembly)
        {
            foreach (AssemblyName asmName in assembly.GetReferencedAssemblies())
            {
                string name = asmName.FullName;
                if (name != thisAssemblyName.FullName && !name.StartsWith("mscorlib") && !name.StartsWith("System") && !name.StartsWith("Microsoft."))
                {
                    asmName.CodeBase = codeBase.Replace(shortName, getShortName(asmName));
                    yield return asmName;
                }
            }
        }
        private IEnumerable<AssemblyName> getReferencedAssembliesOf(IEnumerable<AssemblyName> assemblies)
        {
            foreach (AssemblyName asmName in assemblies)
            {
                Assembly assembly = Assembly.Load(asmName);
                foreach (AssemblyName subRefName in getReferencedAssembliesOf(assembly))
                {
                    yield return subRefName;
                }
            }
        }

        private string getShortName(AssemblyName name)
        {
            return name.FullName.Substring(0, name.FullName.IndexOf(','));
        }

        public override bool Equals(object obj)
        {
            ReferencedLookupAssemblies rla = obj as ReferencedLookupAssemblies;
            return rla != null && object.Equals(assembly, rla.assembly);
        }
        public override int GetHashCode()
        {
            return 1 ^ assembly.GetHashCode();
        }
    }
}
