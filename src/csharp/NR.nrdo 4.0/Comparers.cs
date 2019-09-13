using System;
using System.Collections.Generic;
using System.Linq;

namespace NR.nrdo
{
    [Serializable]
    public abstract class NRDOComparer<TClass> : IComparer<TClass>
    {
        public static NRDOComparer<TClass> Create<TField>(Func<TClass, TField> getter)
            where TField : IComparable<TField>
        {
            return new FieldComparer<TClass, TField>(getter);
        }
        public static NRDOComparer<TClass> Create<TField>(Func<TClass, TField?> getter)
            where TField : struct, IComparable<TField>
        {
            return new NullableFieldComparer<TClass, TField>(getter);
        }
        public static NRDOComparer<TClass> Create<TDeleg>(Func<TClass, TDeleg> getter, NRDOComparer<TDeleg> comparison)
        {
            return new DelegComparer<TClass, TDeleg>(getter, comparison);
        }
        public static NRDOComparer<TClass> Create(params NRDOComparer<TClass>[] comparers)
        {
            return new ChainedComparer<TClass>(comparers);
        }
        public static NRDOComparer<TClass> Create(IEnumerable<NRDOComparer<TClass>> comparers)
        {
            return new ChainedComparer<TClass>(comparers);
        }
        public int Compare(TClass a, TClass b)
        {
            // catch the case where both are null, and also shortcut
            // the case where they happen to be really equal.
            if (object.ReferenceEquals(a, b)) return 0;

            // nulls sort last, always.
            if (a == null) return 1;
            if (b == null) return -1;

            return DoCompare(a, b);
        }
        public NRDOComparer<TClass> Then(NRDOComparer<TClass> then)
        {
            return new ChainedComparer<TClass>(this, then);
        }

        internal abstract int DoCompare(TClass a, TClass b);
        public abstract NRDOComparer<TClass> Reversed {get;}
    }

    [Serializable]
    internal class FieldComparer<TClass, TField> : NRDOComparer<TClass>
        where TField : IComparable<TField>
    {
        protected Func<TClass, TField> getter;

        internal FieldComparer(Func<TClass, TField> getter)
        {
            this.getter = getter;
        }
        internal override int DoCompare(TClass a, TClass b)
        {
            TField af = getter(a);
            TField bf = getter(b);

            if (object.ReferenceEquals(af, bf)) return 0;
            if (af == null) return 1;
            if (bf == null) return -1;

            return af.CompareTo(bf);
        }
        public override NRDOComparer<TClass> Reversed
        {
            get {return new ReverseComparer<TClass, TField>(getter);}
        }
        public override bool Equals(object obj)
        {
            FieldComparer<TClass, TField> fc = obj as FieldComparer<TClass, TField>;
            if (fc == null) return false;
            return Equals(fc);
        }
        public bool Equals(FieldComparer<TClass, TField> fc)
        {
            return getter.Equals(fc.getter) &&
                (this is ReverseComparer<TClass, TField>) == (fc is ReverseComparer<TClass, TField>);
        }
        public override int GetHashCode()
        {
            return getter.GetHashCode();
        }
    }

    [Serializable]
    internal class NullableFieldComparer<TClass, TField> : NRDOComparer<TClass>
        where TField : struct, IComparable<TField>
    {
        protected Func<TClass, TField?> getter;

        internal NullableFieldComparer(Func<TClass, TField?> getter)
        {
            this.getter = getter;
        }
        internal override int DoCompare(TClass a, TClass b)
        {
            TField? af = getter(a);
            TField? bf = getter(b);

            if (af == null && bf == null) return 0;
            if (af == null) return 1;
            if (bf == null) return -1;

            return ((TField) af).CompareTo((TField) bf);
        }
        public override NRDOComparer<TClass> Reversed
        {
            get {return new ReverseNullableComparer<TClass, TField>(getter);}
        }
        public override bool Equals(object obj)
        {
            NullableFieldComparer<TClass, TField> fc = obj as NullableFieldComparer<TClass, TField>;
            if (fc == null) return false;
            return Equals(fc);
        }
        public bool Equals(NullableFieldComparer<TClass, TField> fc)
        {
            return getter.Equals(fc.getter) &&
                (this is ReverseNullableComparer<TClass, TField>) == (fc is ReverseNullableComparer<TClass, TField>);
        }
        public override int GetHashCode()
        {
            return getter.GetHashCode();
        }
    }

    [Serializable]
    internal class ReverseComparer<TClass, TField> : FieldComparer<TClass, TField>
        where TField : IComparable<TField>
    {
        internal ReverseComparer(Func<TClass, TField> getter)
            : base(getter) { }
        internal override int DoCompare(TClass a, TClass b)
        {
            return base.DoCompare(b, a);
        }
        public override NRDOComparer<TClass> Reversed
        {
            get { return new FieldComparer<TClass, TField>(getter); }
        }
    }

    [Serializable]
    internal class ReverseNullableComparer<TClass, TField> : NullableFieldComparer<TClass, TField>
        where TField : struct, IComparable<TField>
    {
        internal ReverseNullableComparer(Func<TClass, TField?> getter)
            : base(getter) { }
        internal override int DoCompare(TClass a, TClass b)
        {
            return base.DoCompare(b, a);
        }
        public override NRDOComparer<TClass> Reversed
        {
            get { return new NullableFieldComparer<TClass, TField>(getter); }
        }
    }

    [Serializable]
    internal class DelegComparer<TClass, TDeleg> : NRDOComparer<TClass>
    {
        private Func<TClass, TDeleg> getter;
        private NRDOComparer<TDeleg> comparison;
        internal DelegComparer(Func<TClass, TDeleg> getter, NRDOComparer<TDeleg> comparison)
        {
            this.getter = getter;
            this.comparison = comparison;
        }
        internal override int DoCompare(TClass a, TClass b)
        {
            return comparison.Compare(getter(a), getter(b));
        }
        public override NRDOComparer<TClass> Reversed
        {
            get { return new DelegComparer<TClass, TDeleg>(getter, comparison.Reversed); }
        }
    }

    [Serializable]
    internal class ChainedComparer<TClass> : NRDOComparer<TClass>
    {
        private IList<NRDOComparer<TClass>> comparers;

        internal ChainedComparer(params NRDOComparer<TClass>[] comparers)
            : this((IEnumerable<NRDOComparer<TClass>>) comparers) { }

        internal ChainedComparer(IEnumerable<NRDOComparer<TClass>> comparers)
        {
            List<NRDOComparer<TClass>> newComparers = new List<NRDOComparer<TClass>>();
            foreach (NRDOComparer<TClass> comparer in comparers)
            {
                ChainedComparer<TClass> cc = comparer as ChainedComparer<TClass>;
                if (cc != null)
                {
                    newComparers.AddRange(cc.comparers);
                }
                else
                {
                    newComparers.Add(comparer);
                }
            }
            this.comparers = newComparers.AsReadOnly();
        }
        internal override int DoCompare(TClass a, TClass b)
        {
            foreach (NRDOComparer<TClass> comparer in comparers)
            {
                int result = comparer.Compare(a, b);
                if (result != 0) return result;
            }
            return 0;
        }

        public override NRDOComparer<TClass> Reversed
        {
            get
            {
                return new ChainedComparer<TClass>(comparers.Reverse());
            }
        }
    }
}