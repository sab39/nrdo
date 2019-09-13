using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NR.nrdo.Reflection
{
    public interface ILookupAssemblies
    {
        IEnumerable<AssemblyName> GetPossibleAssemblies(string tableName);
        IEnumerable<AssemblyName> GetAllKnownAssemblies();
    }
}
