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
            return new CacheStats(GlobalStats.NowStamp, Nrdo.GlobalStats, false, default(ListCacheStats), 0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }
        public static CacheStats CreateList()
        {
            return new CacheStats(GlobalStats.NowStamp, Nrdo.GlobalStats, true, default(ListCacheStats), 0, 0, 0, 0, TimeSpan.Zero, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }

        private CacheStats(TimeSpan startStamp, GlobalStats latestGlobalStats, bool isList, ListCacheStats listStats,
            long hits, long nonHits, long nonHitsOverCapacity, long failures,
            TimeSpan cumulativeTime, TimeSpan cumulativeFailureTime, long cumulativeCost,
            long foreverNonHits, TimeSpan foreverCumulativeTime)
        {
            this.StartStamp = startStamp;
            this.LatestGlobalStats = latestGlobalStats;
            this.IsList = isList;
            this.ListStats = listStats;
            this.Hits = hits;
            this.NonHits = nonHits;
            this.NonHitsOverCapacity = nonHitsOverCapacity;
            this.Failures = failures;
            this.CumulativeTime = cumulativeTime;
            this.CumulativeFailureTime = cumulativeFailureTime;
            this.CumulativeCost = cumulativeCost;
            this.ForeverNonHits = foreverNonHits;
            this.ForeverCumulativeTime = foreverCumulativeTime;
        }

        private TimeSpan StartStamp { get; }

        public GlobalStats LatestGlobalStats { get; }

        // For back-compatibility we can't use a nullable ListStats but it's worth using a struct to reduce GC overhead, so we just
        // populate it unconditionally and track the IsList boolean separately.
        public bool IsList { get; }
        public ListCacheStats ListStats { get; }

        public long Hits { get; }

        public long NonHits { get; }

        public long NonHitsOverCapacity { get; }

        public long Failures { get; }

        public TimeSpan CumulativeTime { get; }

        public TimeSpan CumulativeFailureTime { get; }

        public long CumulativeCost { get; }

        private long ForeverNonHits { get; }
        private TimeSpan ForeverCumulativeTime { get; }

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
                if (ForeverNonHits == 0) return TimeSpan.Zero;
                var resultTicks = ForeverCumulativeTime.Ticks / ForeverNonHits;
                if (ForeverNonHits < 5)
                {
                    var global = Nrdo.GlobalStats;
                    var cap = (2 ^ ForeverNonHits) * global.TotalQueryTime.Ticks / global.CacheNonHitsTotal;
                    if (resultTicks > cap) resultTicks = cap;
                }
                return TimeSpan.FromTicks(resultTicks);
            }
        }

        public long TotalQueries => Hits + NonHits;

        public long NonHitsWithinCapacity => NonHits - NonHitsOverCapacity;

        public Portion Success => Portion.Ratio(Hits, TotalQueries);
        public Portion SuccessWithinCapacity => Portion.Ratio(Hits, Hits + NonHitsOverCapacity);

        public TimeSpan Impact => ForeverNonHits == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Hits * ForeverCumulativeTime.Ticks / ForeverNonHits);

        public TimeSpan Stakes => CumulativeTime + Impact;

        public Portion CostShare => Portion.SafeRatio(CumulativeCost, LatestGlobalStats.CumulativeCost);

        public TimeSpan PotentialImpactGainMax => NonHitsOverCapacity == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(NonHitsOverCapacity * ForeverCumulativeTime.Ticks / ForeverNonHits);
        public TimeSpan PotentialImpactGainEst => NonHitsOverCapacity == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(Hits * NonHitsOverCapacity * ForeverCumulativeTime.Ticks / ((Hits + NonHitsOverCapacity) * ForeverNonHits));
        public TimeSpan PotentialImpactGainHybrid => TimeSpan.FromTicks((PotentialImpactGainMax.Ticks + PotentialImpactGainEst.Ticks) / 2);

        public long Misses => NonHits - ListStats.Skipped;

        // Only really meaningful for the IsList scenario; otherwise just assumes every query returns a single result.
        // Might be interesting to actually track for single-item queries since they can technically return 0 or 1 results but doesn't do so currently
        public double AverageResultItems
        {
            get
            {
                if (!IsList) return 1;
                if (TotalQueries == 0) return 0;
                return (double)ListStats.TotalResultItems / TotalQueries;
            }
        }
        public double WeightedResultItems => IsList ? Math.Sqrt(ListStats.PeakResultItems * AverageResultItems) : 1d;



        private CacheStats withCacheHit(ListCacheStats newListStats)
        {
            return new CacheStats(StartStamp, Nrdo.GlobalStats, IsList, newListStats,
                Hits + 1, // New value
                NonHits,
                NonHitsOverCapacity,
                Failures,
                CumulativeTime,
                CumulativeFailureTime,
                CumulativeCost,
                ForeverNonHits,
                ForeverCumulativeTime);
        }
        private CacheStats withCacheNonHit(ListCacheStats newListStats, TimeSpan queryTime, bool isOverCapacity)
        {
            return new CacheStats(StartStamp, Nrdo.GlobalStats, IsList, newListStats,
                Hits,
                NonHits + 1,
                isOverCapacity ? NonHitsOverCapacity + 1 : NonHitsOverCapacity,
                Failures,
                CumulativeTime + NrdoStats.nonZeroTime(queryTime),
                CumulativeFailureTime,
                CumulativeCost,
                ForeverNonHits + 1,
                ForeverCumulativeTime + NrdoStats.nonZeroTime(queryTime));
        }

        public CacheStats WithSingleCacheHit()
        {
            return withCacheHit(ListStats.WithNewResult());
        }
        public CacheStats WithSingleCacheNonHit(TimeSpan queryTime, bool isOverCapacity)
        {
            return withCacheNonHit(ListStats.WithNewResult(), queryTime, isOverCapacity);
        }

        public CacheStats WithListCacheHit(int resultItems)
        {
            return withCacheHit(ListStats.WithNewResult(resultItems, false));
        }
        public CacheStats WithListCacheMiss(int resultItems, TimeSpan queryTime, bool isOverCapacity)
        {
            return withCacheNonHit(ListStats.WithNewResult(resultItems, false), queryTime, isOverCapacity);
        }
        public CacheStats WithListCacheSkip(int resultItems, TimeSpan queryTime)
        {
            return withCacheNonHit(ListStats.WithNewResult(resultItems, true), queryTime, true);
        }

        public CacheStats WithFailure(TimeSpan failTime)
        {
            return new CacheStats(StartStamp, LatestGlobalStats, IsList, ListStats,
                Hits,
                NonHits,
                NonHitsOverCapacity,
                Failures + 1, // New value
                CumulativeTime,
                CumulativeFailureTime + NrdoStats.nonZeroTime(failTime), // New value
                CumulativeCost,
                ForeverNonHits,
                ForeverCumulativeTime);
        }

        public CacheStats WithCycleCost(int cost)
        {
            return new CacheStats(StartStamp, LatestGlobalStats, IsList, ListStats,
                Hits,
                NonHits,
                NonHitsOverCapacity,
                Failures,
                CumulativeTime,
                CumulativeFailureTime,
                CumulativeCost + cost, // New Value
                ForeverNonHits,
                ForeverCumulativeTime);
        }

        public CacheStats Since(CacheStats prior)
        {
            if (prior == null) return this;

            return new CacheStats(prior.LatestGlobalStats.LatestOperationStamp, LatestGlobalStats,
                IsList, ListStats.Since(prior.ListStats),
                Hits - prior.Hits,
                NonHits - prior.NonHits,
                NonHitsOverCapacity - prior.NonHitsOverCapacity,
                Failures - prior.Failures,
                CumulativeTime - prior.CumulativeTime,
                CumulativeFailureTime - prior.CumulativeFailureTime,
                CumulativeCost - prior.CumulativeCost,
                ForeverNonHits, // The "forever" values don't get subtracted because calculation of average time requires them to never drop to zero
                ForeverCumulativeTime);
        }

        public CacheStats ToNow()
        {
            return new CacheStats(StartStamp, Nrdo.GlobalStats.ToNow(), IsList, ListStats,
                Hits,
                NonHits,
                NonHitsOverCapacity,
                Failures,
                CumulativeTime,
                CumulativeFailureTime,
                CumulativeCost,
                ForeverNonHits,
                ForeverCumulativeTime);
        }
    }
}
