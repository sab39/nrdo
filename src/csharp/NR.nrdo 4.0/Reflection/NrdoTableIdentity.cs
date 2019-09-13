using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoTableIdentity
    {
        public static NrdoTableIdentity<T> Get<T>()
            where T : DBTableObject<T>
        {
            return NrdoTableIdentity<T>.Instance;
        }
        public static NrdoTableIdentity GetDynamic(Type tableType)
        {
            var nameType = typeof(NrdoTableIdentity<>).MakeGenericType(tableType);
            var prop = nameType.GetProperty("Instance", BindingFlags.Static | BindingFlags.NonPublic);
            return (NrdoTableIdentity)prop.GetValue(null, null);
        }

        public override string ToString()
        {
            return DbName;
        }

        public abstract string DbName { get; }
        public abstract string QuotedDbName { get; }
        public abstract string NrdoName { get; }
        public abstract long ModificationCount { get; }
        public abstract event Action ModificationAny;
        public abstract event Action ModificationFullFlush;
        public abstract void InvokeTypedMethod(ITypedMethod method);
        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();
    }

    public sealed class NrdoTableIdentity<T> : NrdoTableIdentity
        where T : DBTableObject<T>
    {
        internal static NrdoTableIdentity<T> Instance = new NrdoTableIdentity<T>();
        private NrdoTableIdentity() { }

        private NrdoTable nrdoTable;
        public NrdoTable NrdoTable
        {
            get
            {
                if (nrdoTable == null) nrdoTable = NrdoTable.GetTable<T>();
                return nrdoTable;
            }
        }

        public override string DbName
        {
            get { return NrdoTable.DatabaseName; }
        }

        public override string QuotedDbName
        {
            get { return "[" + DbName + "]"; }
        }

        public override string NrdoName
        {
            get { return NrdoTable.Name; }
        }

        public override long ModificationCount
        {
            get { return DBTableObject<T>.DataModification.Count; }
        }

        public override event Action ModificationAny
        {
            add { DBTableObject<T>.DataModification.Any += value; }
            remove { DBTableObject<T>.DataModification.Any -= value; }
        }
        public override event Action ModificationFullFlush
        {
            add { DBTableObject<T>.DataModification.FullFlush += value; }
            remove { DBTableObject<T>.DataModification.FullFlush -= value; }
        }

        public override void InvokeTypedMethod(ITypedMethod method)
        {
            method.Invoke<T>(null);
        }

        public override bool Equals(object obj)
        {
            return obj is NrdoTableIdentity<T>;
        }

        public override int GetHashCode()
        {
            return typeof(T).GetHashCode();
        }
    }
}
