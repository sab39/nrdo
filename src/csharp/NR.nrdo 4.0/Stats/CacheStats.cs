using NR.nrdo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public sealed class CacheStats
    {
        public static CacheStats CreateSingle()
        {
            return new CacheStats(GlobalStats.NowStamp, Nrdo.GlobalStats, null, 0, 0, 0, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }
        public static CacheStats CreateList()
        {
            return new CacheStats(GlobalStats.NowStamp, Nrdo.GlobalStats, new ListCacheStats(0, 0, 0), 0, 0, 0, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }

        private CacheStats(TimeSpan startStamp, GlobalStats latestGlobalStats, ListCacheStats listStats, long hits, long nonHits, long nonHitsOverCapacity, TimeSpan cumulativeTime, long cumulativeCost, long foreverNonHits, TimeSpan foreverCumulativeTime)
        {
            this.StartStamp = startStamp;
            this.latestGlobalStats = latestGlobalStats;
            this.listStats = listStats == null ? null : listStats.ForCacheStats(this);
            this.hits = hits;
            this.nonHits = nonHits;
            this.nonHitsOverCapacity = nonHitsOverCapacity;
            this.cumulativeTime = cumulativeTime;
            this.cumulativeCost = cumulativeCost;
            this.foreverNonHits = foreverNonHits;
            this.foreverCumulativeTime = foreverCumulativeTime;
        }

        private TimeSpan StartStamp { get; }

        private readonly GlobalStats latestGlobalStats;
        public GlobalStats LatestGlobalStats { get { return latestGlobalStats; } }

        private readonly ListCacheStats listStats;
        public ListCacheStats ListStats { get { return listStats; } }

        private readonly long hits;
        public long Hits { get { return hits; } }

        private readonly long nonHits;
        public long NonHits { get { return nonHits; } }

        private readonly long nonHitsOverCapacity;
        public long NonHitsOverCapacity { get { return nonHitsOverCapacity; } }

        private readonly TimeSpan cumulativeTime;
        public TimeSpan CumulativeTime { get { return cumulativeTime; } }

        private readonly long cumulativeCost;
        public long CumulativeCost { get { return cumulativeCost; } }

        private readonly long foreverNonHits;
        private readonly TimeSpan foreverCumulativeTime;

        // We preserve the "forever" total number of nonhits and cumulative time as well as the "current" values and
        // use those instead of the ones within the current timespan for calculating average time and
        // other calculations that are supposed to be based on average time. This is because
        // NonHits within an individual timerange can be zero even if Hits is nonzero; foreverNonHits can't.
        // If the number of nonHits ever is less than five, there's a chance the calculated time for the query is an outlier due to startup overhead.
        // In that case we cap the calculated average at 2^foreverNonHits times the global average query time.
        public TimeSpan AverageTime
        {
            get
            {
                if (foreverNonHits == 0) return TimeSpan.Zero;
                var resultTicks = foreverCumulativeTime.Ticks / foreverNonHits;
                if (foreverNonHits < 5)
                {
                    var global = Nrdo.GlobalStats;
                    var cap = (2 ^ foreverNonHits) * global.TotalQueryTime.Ticks / global.CacheNonHitsTotal;
                    if (resultTicks > cap) resultTicks = cap;
                }
                return TimeSpan.FromTicks(resultTicks);
            }
        }

        public long TotalQueries { get { return Hits + NonHits; } }

        public long NonHitsWithinCapacity { get { return NonHits - nonHitsOverCapacity; } }

        public Portion Success { get { return Portion.Ratio(Hits, TotalQueries); } }
        public Portion SuccessWithinCapacity { get { return Portion.Ratio(Hits, Hits + NonHitsOverCapacity); } }

        public TimeSpan Impact { get { return foreverNonHits == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Hits * foreverCumulativeTime.Ticks / foreverNonHits); } }

        public TimeSpan Stakes { get { return CumulativeTime + Impact; } }

        public Portion CostShare { get { return Portion.SafeRatio(CumulativeCost, LatestGlobalStats.CumulativeCost); } }

        public TimeSpan PotentialImpactGainMax { get { return NonHitsOverCapacity == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(NonHitsOverCapacity * foreverCumulativeTime.Ticks / foreverNonHits); } }
        public TimeSpan PotentialImpactGainEst { get { return NonHitsOverCapacity == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Hits * NonHitsOverCapacity * foreverCumulativeTime.Ticks / ((Hits + NonHitsOverCapacity) * foreverNonHits)); } }
        public TimeSpan PotentialImpactGainHybrid { get { return TimeSpan.FromTicks((PotentialImpactGainMax.Ticks + PotentialImpactGainEst.Ticks) / 2); } }

        public bool IsList { get { return listStats != null; } }

        private CacheStats withCacheHit(ListCacheStats newListStats)
        {
            if (listStats != null && newListStats == null) throw new ApplicationException("Not a single cache!");
            if (listStats == null && newListStats != null) throw new ApplicationException("Not a list cache!");

            return new CacheStats(StartStamp, Nrdo.GlobalStats, newListStats,
                hits + 1, // New value
                nonHits,
                nonHitsOverCapacity,
                cumulativeTime,
                cumulativeCost,
                foreverNonHits,
                foreverCumulativeTime);
        }
        private CacheStats withCacheNonHit(ListCacheStats newListStats, TimeSpan queryTime, bool isOverCapacity)
        {
            if (listStats != null && newListStats == null) throw new ApplicationException("Not a single cache!");
            if (listStats == null && newListStats != null) throw new ApplicationException("Not a list cache!");

            return new CacheStats(StartStamp, Nrdo.GlobalStats, newListStats,
                hits,
                nonHits + 1,
                isOverCapacity ? nonHitsOverCapacity + 1 : nonHitsOverCapacity,
                cumulativeTime + NrdoStats.nonZeroTime(queryTime),
                cumulativeCost,
                foreverNonHits + 1,
                foreverCumulativeTime + NrdoStats.nonZeroTime(queryTime));
        }

        public CacheStats WithSingleCacheHit()
        {
            return withCacheHit(null);
        }
        public CacheStats WithSingleCacheNonHit(TimeSpan queryTime, bool isOverCapacity)
        {
            return withCacheNonHit(null, queryTime, isOverCapacity);
        }

        public CacheStats WithListCacheHit(int resultItems)
        {
            return withCacheHit(listStats.WithNewResult(resultItems, false));
        }
        public CacheStats WithListCacheMiss(int resultItems, TimeSpan queryTime, bool isOverCapacity)
        {
            return withCacheNonHit(listStats.WithNewResult(resultItems, false), queryTime, isOverCapacity);
        }
        public CacheStats WithListCacheSkip(int resultItems, TimeSpan queryTime)
        {
            return withCacheNonHit(listStats.WithNewResult(resultItems, true), queryTime, true);
        }

        public CacheStats WithCycleCost(int cost)
        {
            return new CacheStats(StartStamp, latestGlobalStats, listStats,
                hits,
                nonHits,
                nonHitsOverCapacity,
                cumulativeTime,
                cumulativeCost + cost,
                foreverNonHits,
                foreverCumulativeTime);
        }

        public CacheStats Since(CacheStats prior)
        {
            if (prior == null) return this;

            return new CacheStats(prior.latestGlobalStats.LatestOperationStamp, latestGlobalStats,
                listStats == null ? null : listStats.Since(prior.listStats),
                hits - prior.hits,
                nonHits - prior.nonHits,
                nonHitsOverCapacity - prior.nonHitsOverCapacity,
                cumulativeTime - prior.cumulativeTime,
                cumulativeCost - prior.cumulativeCost,
                foreverNonHits, // The "forever" values don't get subtracted because calculation of average time requires them to never drop to zero
                foreverCumulativeTime);
        }

        public CacheStats ToNow()
        {
            return new CacheStats(StartStamp, Nrdo.GlobalStats.ToNow(), listStats,
                hits,
                nonHits,
                nonHitsOverCapacity,
                cumulativeTime,
                cumulativeCost,
                foreverNonHits,
                foreverCumulativeTime);
        }
    }
}
