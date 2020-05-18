using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public sealed class ListCacheStats
    {
        internal ListCacheStats(long skipped, long totalResultItems, int peakResultItems)
        {
            this.skipped = skipped;
            this.totalResultItems = totalResultItems;
            this.peakResultItems = peakResultItems;
        }
        private ListCacheStats(ListCacheStats proto, CacheStats cacheStats)
            : this(proto.skipped, proto.totalResultItems, proto.peakResultItems)
        {
            this.cacheStats = cacheStats;
        }
        internal ListCacheStats ForCacheStats(CacheStats cacheStats)
        {
            return new ListCacheStats(this, cacheStats);
        }

        private readonly CacheStats cacheStats;

        private readonly long skipped;
        public long Skipped { get { return skipped; } }

        private readonly long totalResultItems;
        public long TotalResultItems { get { return totalResultItems; } }

        private readonly int peakResultItems;
        public int PeakResultItems { get { return peakResultItems; } }

        public long Misses { get { return cacheStats.NonHits - Skipped; } }

        public double AverageResultItems { get { return cacheStats.TotalQueries == 0 ? 0 : (double)TotalResultItems / cacheStats.TotalQueries; } }
        public double WeightedResultItems { get { return Math.Sqrt(PeakResultItems * AverageResultItems); } }

        internal ListCacheStats WithNewResult(int resultItems, bool wasSkip)
        {
            return new ListCacheStats(wasSkip ? skipped + 1 : skipped, totalResultItems + resultItems + 1, Math.Max(peakResultItems, resultItems + 1));
        }

        public ListCacheStats Since(ListCacheStats prior)
        {
            if (prior == null) return this;

            return new ListCacheStats(
                skipped - prior.skipped,
                totalResultItems - prior.totalResultItems,
                peakResultItems);
        }
    }
}
