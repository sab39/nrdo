using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Caching
{
    internal sealed class AllWhere<T> : CachingWhere<T, AllWhere<T>, CacheAll<T>>
        where T : DBTableObject<T>
    {
        public override void SetOnCmd(NrdoCommand cmd)
        {
        }
        public override string SQLStatement
        {
            get { return DBTableObject<T>.selectStatement; }
        }
        public override string GetMethodName
        {
            get { return typeof(T).FullName + ".GetAll"; }
        }
        public override int GetHashCode()
        {
            return typeof(T).GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is AllWhere<T>;
        }
    }
    internal sealed class CacheAll<T> : TableMultiObjectCache<T, AllWhere<T>, CacheAll<T>>
        where T : DBTableObject<T>
    {
        public CacheAll()
            : base(0, 100)
        {
            DBTableObject<T>.DataModification.CascadeDelete += Clear;
        }
        public override long ModificationCountHash { get { return DBTableObject<T>.DataModification.Count; } }
    }
}
