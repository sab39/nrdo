using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NR.nrdo.Connection;

namespace NR.nrdo.Caching
{
    public class DBObjectModificationCache<T> : DBObjectCacheBase<T, DBObjectModificationCache<T>.ModifyWhere, DBObjectModificationCache<T>>
        where T : DBObject<T>
    {
        public DBObjectModificationCache(string methodName)
        {
            this.MethodName = methodName;
        }
        public string MethodName { get; }

        protected override string GetMethodName()
        {
            return $"{typeof(T).FullName}.{MethodName}";
        }

        // A 'Where' class just needs to exist for the APIs to believe in it, but should never be instantiated
        public class ModifyWhere : CachingWhereBase<T, ModifyWhere, DBObjectModificationCache<T>>
        {
            private ModifyWhere() { throw new NotImplementedException(); }

            public override IDBObjectCache<T> Cache => throw new NotImplementedException();
            public override void SetOnCmd(NrdoCommand cmd) => throw new NotImplementedException();
            public override string SQLStatement => throw new NotImplementedException();
            public override string GetMethodName => throw new NotImplementedException();
        }

        protected override bool IsDisabled => true;
        public override long ModificationCountHash => 0;

        public override void Clear()
        {
            // No actual cache so nothing to clear
        }

        public override int Capacity
        {
            get => 0;
            set { }
        }

        public override int Count => 0;
        public override int FlushCount => 0;
        public override int PeakCount => 0;
        public override int Cost => 0;
        public override int PeakCost => 0;

        public override int CyclePeakCost() => 0;
        public override bool IsOverflowing => false;
    }
}
