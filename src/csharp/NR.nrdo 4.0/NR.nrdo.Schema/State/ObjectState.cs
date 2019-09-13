using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;

namespace NR.nrdo.Schema.State
{
    public abstract class ObjectState
    {
        // Can only be subclassed within this assembly, specifically by RootObjectState and SubObjectState
        internal ObjectState()
        {
        }

        public abstract ObjectType ObjectType { get; }
        public abstract override string ToString();
    }
}
