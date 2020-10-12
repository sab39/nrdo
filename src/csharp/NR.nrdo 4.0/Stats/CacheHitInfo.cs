using NR.nrdo.Caching;
using NR.nrdo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public class CacheHitInfo : IComparable<CacheHitInfo>
    {
        private string method;
        public string Method { get { return method; } }
        private IDBObjectCache cache;
        public IDBObjectCache Cache { get { return cache; } }

        private CacheStats cacheStats;
        public CacheStats CacheStats { get { return cacheStats; } }
        public bool IsList { get { return cache is IListCache; } }

        internal CacheStats lastShrinkCycleStats;
        public CacheStats LastShrinkCycleStats { get { return lastShrinkCycleStats; } }

        internal CacheStats lastShrunkStats;
        public CacheStats LastShrunkStats { get { return lastShrunkStats; } }

        internal CacheStats lastGrownStats;
        public CacheStats LastGrownStats { get { return lastGrownStats; } }

        internal int growCount;
        public int GrowCount { get { return growCount; } }

        internal int shrinkCount;
        public int ShrinkCount { get { return shrinkCount; } }

        public CacheStats LastChangeStats
        {
            get
            {
                if (lastGrownStats == null) return lastShrunkStats;
                if (lastShrunkStats == null) return lastGrownStats;

                var grown = lastGrownStats.LatestGlobalStats.LatestOperationStamp;
                var shrunk = lastShrunkStats.LatestGlobalStats.LatestOperationStamp;
                return grown > shrunk ? lastGrownStats : lastShrunkStats;
            }
        }

        public CacheStats CacheStatsSinceChange { get { return CacheStats.ToNow().Since(LastChangeStats); } }
        public CacheStats CacheStatsSinceGrow { get { return CacheStats.ToNow().Since(LastGrownStats); } }
        public CacheStats CacheStatsSinceShrink { get { return CacheStats.ToNow().Since(LastShrunkStats); } }

        internal void updateStats(Func<GlobalStats, GlobalStats> globalUpdate, Func<CacheStats, CacheStats> statsUpdate)
        {
            lock (Nrdo.LockObj)
            {
                NrdoStats.UpdateGlobalStats(globalUpdate);
                cacheStats = statsUpdate(cacheStats);
                NrdoStats.allowCacheRebalance();
            }
        }

        // New code should go through CacheStats/ListCacheStats directly, but these old properties need to exist for
        // backward compatibility, and have to be ints rather than longs for the same reason.
        public TimeSpan CumulativeTime { get { return CacheStats.CumulativeTime; } }
        public TimeSpan CumulativeFailureTime { get { return CacheStats.CumulativeFailureTime; } }
        public TimeSpan AverageTime { get { return CacheStats.AverageTime; } }
        public int Hits { get { return (int)CacheStats.Hits; } }
        public int Misses { get { return (int)CacheStats.Misses; } }
        public int OverflowingMisses { get { return (int)CacheStats.NonHitsOverCapacity; } }
        public int Skipped { get { return (int)CacheStats.ListStats.Skipped; } }
        public int Failures { get { return (int)CacheStats.Failures; } }

        internal int tweakedDirectly;
        public int TweakedDirectly { get { return tweakedDirectly; } }
        internal int iterated;
        public int Iterated { get { return iterated; } }
        internal int tweakedByIteration;
        public int TweakedByIteration { get { return tweakedByIteration; } }

        private int itemCount
        {
            get
            {
                var multiCache = Cache as IListCache;
                return multiCache != null ? multiCache.ItemCount : Cache.Count;
            }
        }

        public int CycleCost()
        {
            var result = cache.CyclePeakCost();
            cacheStats = cacheStats.WithCycleCost(result);
            return result;
        }

        public CacheHitInfo(string method, IDBObjectCache cache)
        {
            this.method = method;
            this.cache = cache;
            this.cacheStats = cache is IListCache ? CacheStats.CreateList() : CacheStats.CreateSingle();
        }

        public int CompareTo(CacheHitInfo info)
        {
            int result = info.CumulativeFailureTime.CompareTo(CumulativeFailureTime);
            if (result == 0) result = (info.Failures).CompareTo(Failures);
            if (result == 0) result = info.CumulativeTime.CompareTo(CumulativeTime);
            if (result == 0) result = (info.Misses + info.Skipped).CompareTo(Misses + Skipped);
            if (result == 0) result = info.Misses.CompareTo(Misses);
            if (result == 0) result = info.OverflowingMisses.CompareTo(OverflowingMisses);
            if (result == 0) result = info.Hits.CompareTo(Hits);
            if (result == 0) result = Method.CompareTo(info.Method);
            return result;
        }
    }
}
