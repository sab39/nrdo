using NR.nrdo.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public class CacheBaseline
    {
        // 81% (90% of 90%) of the cumulative cost divided by the number of caches that it takes to cumulatively make up 90% of the total cost
        private readonly Portion cost;
        public Portion Cost { get { return cost; } }

        private readonly int pulseCount;
        public int PulseCount { get { return pulseCount; } }

        // Baseline impact is the impact of the median cache (with each cache weighted by number of calls for purposes of finding the "middle")
        private readonly TimeSpan impact;
        public TimeSpan Impact { get { return impact; } }

        // We store the total global number of queries over the timespan we are measuring so that Impact can be normalized per-million-queries
        private readonly long queryCount;
        public long QueryCount { get { return queryCount; } }

        internal CacheBaseline(Portion cost, int pulseCount, TimeSpan impact, long queryCount)
        {
            this.cost = cost;
            this.pulseCount = pulseCount;
            this.impact = impact;
            this.queryCount = queryCount;
        }

        public Portion CostPerPulse { get { return pulseCount == 0 ? Portion.ZeroOfZero : cost / pulseCount; } }
        public TimeSpan ImpactPerMillionQueries { get { return queryCount == 0 ? TimeSpan.Zero : TimeSpan.FromTicks(impact.Ticks * 1000000 / queryCount); } }
    }
}
