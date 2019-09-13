using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public abstract class DBSingleObjectCache<T, TWhere, TCache> : DBObjectCacheBase<T, TWhere, TCache>, IDBSingleObjectCache<T>
        where T : DBObject<T>
        where TWhere : CachingWhereBase<T, TWhere, TCache>
        where TCache : DBSingleObjectCache<T, TWhere, TCache>
    {
        public DBSingleObjectCache(int capacity)
        {
            LruCache = new LruCache<Where<T>, T>(capacity);
        }
        protected LruCache<Where<T>, T> LruCache { get; private set; }

        public bool TryGetValue(Where<T> where, out T result)
        {
            return LruCache.TryGetValue(where, out result);
        }

        public void StoreValue(Where<T> where, T result)
        {
            if (IsEnabled) LruCache[where] = result;
        }

        public override void Clear()
        {
            LruCache.Clear();
        }

        public override int Capacity
        {
            get { return IsEnabled ? LruCache.Capacity : 0; }
            set { LruCache.Capacity = value; }
        }

        protected override bool IsDisabled
        {
            get { return LruCache.Capacity <= 0; }
        }

        public override int Count
        {
            get { return LruCache.Count; }
        }

        public override int Cost { get { return LruCache.Cost; } }
        public override int PeakCost { get { return LruCache.PeakCost; } }
        public override int CyclePeakCost() { return LruCache.CyclePeakCost(); }

        public override int FlushCount
        {
            get { return LruCache.FlushCount; }
        }

        public override int PeakCount
        {
            get { return LruCache.PeakCount; }
        }

        public override bool IsOverflowing
        {
            get { return LruCache.IsOverflowing; }
        }
    }
}
