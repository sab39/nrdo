using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public abstract class DBMultiObjectCache<T, TWhere, TCache> : DBObjectCacheBase<T, TWhere, TCache>, IDBMultiObjectCache<T>
        where T : DBObject<T>
        where TWhere : CachingWhereBase<T, TWhere, TCache>
        where TCache : DBMultiObjectCache<T, TWhere, TCache>
    {
        protected DBMultiObjectCache(int capacity, int itemCapacity)
        {
            LruCache = new ListLruCache<Where<T>, T>(capacity, itemCapacity);
        }
        protected ListLruCache<Where<T>, T> LruCache { get; private set; }

        public bool TryGetValue(Where<T> where, out List<T> result)
        {
            return LruCache.TryGetValue(where, out result);
        }

        public void StoreValue(Where<T> where, List<T> result)
        {
            if (IsEnabled) LruCache[where] = result;
        }

        public override void Clear()
        {
            LruCache.Clear();
        }

        protected override bool IsDisabled
        {
            get { return LruCache.Capacity <= 0 && LruCache.ItemCapacity <= 0; }
        }

        public override int Capacity
        {
            get { return IsEnabled ? LruCache.Capacity : 0; }
            set { LruCache.Capacity = value; }
        }

        public override int Count
        {
            get { return LruCache.Count; }
        }

        public override int FlushCount
        {
            get { return LruCache.FlushCount; }
        }

        public override int PeakCount
        {
            get { return LruCache.PeakCount; }
        }

        public int ItemCapacity
        {
            get { return IsEnabled ? LruCache.ItemCapacity : 0; ; }
            set { LruCache.ItemCapacity = value; }
        }

        public int ItemCount
        {
            get { return LruCache.ItemCount; }
        }

        public int PeakItemCount
        {
            get { return LruCache.ItemCount; }
        }

        public override bool IsOverflowing
        {
            get { return LruCache.IsOverflowing; }
        }

        public override int Cost { get { return LruCache.Cost; } }
        public override int PeakCost { get { return LruCache.PeakCost; } }
        public override int CyclePeakCost() { return LruCache.CyclePeakCost(); }
    }
}
