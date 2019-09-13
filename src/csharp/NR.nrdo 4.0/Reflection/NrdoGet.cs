using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoGet : NrdoGetBase, IComparable<NrdoGet>
    {
        // Cannot be subclassed outside this assembly
        internal NrdoGet(NrdoTable table, MethodInfo method, NrdoGetAttribute gattr)
            : base(table, method, gattr)
        {
            this.index = gattr.Index;
            this.hasIndex = gattr.HasIndex;
        }

        private int index;

        internal override string DfnSyntaxPrefix { get { return "get"; } }
        internal override bool InternalHasIndex { get { return HasIndex; } }

        private bool hasIndex;
        public bool HasIndex { get { return hasIndex; } }

        int IComparable<NrdoGet>.CompareTo(NrdoGet other)
        {
            return index.CompareTo(other.index);
        }
    }

    public sealed class NrdoSingleGet : NrdoGet
    {
        internal NrdoSingleGet(NrdoTable table, MethodInfo method, NrdoGetAttribute gattr)
            : base(table, method, gattr) { }

        public override bool IsMulti { get { return false; } }
        public ITableObject Call(params object[] args)
        {
            if (!HasCode) throw new InvalidOperationException(Table.Name + " get by " + Name + " has no code, so cannot be called");
            return (ITableObject) Method.Invoke(null, args);
        }

        public bool IsPkey { get { return this == Table.PkeyGet; } }
    }

    public sealed class NrdoMultiGet : NrdoGet
    {
        internal NrdoMultiGet(NrdoTable table, MethodInfo method, NrdoGetAttribute gattr)
            : base(table, method, gattr) { }

        public override bool IsMulti { get { return true; } }
        public IList<ITableObject> Call(params object[] args)
        {
            if (!HasCode) throw new InvalidOperationException(Table.Name + " get by " + Name + " has no code, so cannot be called");
            var objects = (System.Collections.IList)Method.Invoke(null, args);
            return objects.Cast<ITableObject>().ToList().AsReadOnly();
        }
    }
}
