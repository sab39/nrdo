using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public struct ListCacheStats
    {
        internal ListCacheStats(long skipped, long totalResultItems, int peakResultItems)
        {
            this.Skipped = skipped;
            this.TotalResultItems = totalResultItems;
            this.PeakResultItems = peakResultItems;
        }

        public long Skipped { get; }

        public long TotalResultItems { get; }

        public int PeakResultItems { get; }

        internal ListCacheStats WithNewResult(int resultItems = 0, bool wasSkip = false)
        {
            return new ListCacheStats(
                wasSkip ? Skipped + 1 : Skipped,
                TotalResultItems + resultItems + 1,
                Math.Max(PeakResultItems, resultItems + 1));
        }

        public ListCacheStats Since(ListCacheStats prior)
        {
            return new ListCacheStats(
                Skipped - prior.Skipped,
                TotalResultItems - prior.TotalResultItems,
                PeakResultItems);
        }
    }
}
