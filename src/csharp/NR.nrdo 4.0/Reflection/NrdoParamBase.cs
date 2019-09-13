using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoParamBase
    {
        // Cannot be subclassed outside this assembly
        internal NrdoParamBase() { }

        public abstract string Name { get; }
        public abstract Type Type { get; }
        public abstract bool IsNullable { get; }
    }
}
