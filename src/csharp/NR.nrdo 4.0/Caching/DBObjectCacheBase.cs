using NR.nrdo.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Caching
{
    public abstract class DBObjectCacheBase<T, TWhere, TCache> : IDBObjectCache<T>
        where T : DBObject<T>
        where TWhere : CachingWhereBase<T, TWhere, TCache>
        where TCache : DBObjectCacheBase<T, TWhere, TCache>
    {
        protected const int IterationLimit = 100; // Don't do things that involve iterating on caches larger than this - just clear instead

        private CacheHitInfo hitInfo;
        public DBObjectCacheBase()
        {
            Nrdo.RegisterCache<T, TWhere, TCache>((TCache)this);
        }

        protected abstract bool IsDisabled { get; }

        public virtual bool IsEnabled
        {
            get { return Nrdo.CachingEnabled && !IsDisabled; }
        }

        protected virtual string GetMethodName()
        {
            return typeof(T).FullName + "." + typeof(TCache).Name.Replace("Cache", "Get");
        }

        public CacheHitInfo HitInfo
        {
            get
            {
                lock (Nrdo.LockObj)
                {
                    if (hitInfo == null) hitInfo = new CacheHitInfo(GetMethodName(), this);
                    return hitInfo;
                }
            }
        }

        public abstract long ModificationCountHash { get; }

        public abstract void Clear();
        public abstract int Capacity { get; set; }
        public abstract int Count { get; }
        public abstract int FlushCount { get; }
        public abstract int PeakCount { get; }
        public abstract int Cost { get; }
        public abstract int PeakCost { get; }
        public abstract int CyclePeakCost();
        public abstract bool IsOverflowing { get; }
    }
}
