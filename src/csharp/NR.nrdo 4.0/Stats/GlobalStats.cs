using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public sealed class GlobalStats
    {
        private static readonly Stopwatch globalStopwatch = Stopwatch.StartNew();
        internal static TimeSpan NowStamp => globalStopwatch.Elapsed;

        public GlobalStats()
        {
            this.StartStamp = TimeSpan.Zero;
            this.LatestOperationStamp = TimeSpan.Zero;
        }

        private GlobalStats(TimeSpan startStamp, TimeSpan latestOperationStamp,
            long cacheHitsTotal, long cacheMissesTotal, long cacheSkippedTotal, long totalModifications, long totalFailures,
            TimeSpan totalQueryTime, TimeSpan totalModificationTime, TimeSpan totalFailureTime,
            long cumulativeCost, long scopeStarts, long connectionStarts, long transactionStarts)
        {
            this.StartStamp = startStamp;
            this.LatestOperationStamp = latestOperationStamp;
            this.CacheHitsTotal = cacheHitsTotal;
            this.CacheMissesTotal = cacheMissesTotal;
            this.CacheSkippedTotal = cacheSkippedTotal;
            this.TotalModifications = totalModifications;
            this.TotalFailures = totalFailures;
            this.TotalQueryTime = totalQueryTime;
            this.TotalModificationTime = totalModificationTime;
            this.TotalFailureTime = totalFailureTime;
            this.CumulativeCost = cumulativeCost;
            this.ScopeStarts = scopeStarts;
            this.ConnectionStarts = connectionStarts;
            this.TransactionStarts = transactionStarts;
        }

        public TimeSpan StartStamp { get; }
        public TimeSpan LatestOperationStamp { get; }

        public long CacheHitsTotal { get; }

        public long CacheMissesTotal { get; }

        public long CacheSkippedTotal { get; }

        public long TotalQueries => CacheHitsTotal + CacheMissesTotal + CacheSkippedTotal;
        public long CacheNonHitsTotal => CacheMissesTotal + CacheSkippedTotal;

        public long TotalModifications { get; }

        public long TotalFailures { get; }

        public long TotalOperations => TotalQueries + TotalModifications + TotalFailures;

        public TimeSpan TotalQueryTime { get; } // DB hits only; cache hits are presumed instantaneous

        public TimeSpan TotalModificationTime { get; }

        public TimeSpan TotalFailureTime { get; }

        public TimeSpan TotalDBTime => TotalQueryTime + TotalModificationTime + TotalFailureTime;

        public long CumulativeCost { get; }

        public long ScopeStarts { get; }

        public long ConnectionStarts { get; }

        public long TransactionStarts { get; }

        public GlobalStats WithCacheHit()
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal + 1, // New value
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithCacheMiss(TimeSpan queryTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal + 1, // New value
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime + NrdoStats.nonZeroTime(queryTime), // New value
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithCacheSkip(TimeSpan queryTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal + 1, // New value
                TotalModifications,
                TotalFailures,
                TotalQueryTime + NrdoStats.nonZeroTime(queryTime), // New value
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithModification(TimeSpan modTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications + 1, // New value
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime + NrdoStats.nonZeroTime(modTime), // New value
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithFailure(TimeSpan failTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures + 1, // New value
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime + NrdoStats.nonZeroTime(failTime), // New value
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithCycleCost(long cost)
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost + cost, // new value
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithScopeStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts + 1, // new value
                ConnectionStarts,
                TransactionStarts);
        }

        public GlobalStats WithConnectionStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts + 1, // new value
                TransactionStarts);
        }

        public GlobalStats WithTransactionStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts + 1); // new value
        }

        public GlobalStats Since(GlobalStats prior)
        {
            return new GlobalStats(prior.LatestOperationStamp, LatestOperationStamp,
                CacheHitsTotal - prior.CacheHitsTotal,
                CacheMissesTotal - prior.CacheMissesTotal,
                CacheSkippedTotal - prior.CacheSkippedTotal,
                TotalModifications - prior.TotalModifications,
                TotalFailures - prior.TotalFailures,
                TotalQueryTime - prior.TotalQueryTime,
                TotalModificationTime - prior.TotalModificationTime,
                TotalFailureTime - prior.TotalFailureTime,
                CumulativeCost - prior.CumulativeCost,
                ScopeStarts - prior.ScopeStarts,
                ConnectionStarts - prior.ConnectionStarts,
                TransactionStarts - prior.TransactionStarts);
        }

        public GlobalStats ToNow()
        {
            return new GlobalStats(StartStamp, NowStamp,
                CacheHitsTotal,
                CacheMissesTotal,
                CacheSkippedTotal,
                TotalModifications,
                TotalFailures,
                TotalQueryTime,
                TotalModificationTime,
                TotalFailureTime,
                CumulativeCost,
                ScopeStarts,
                ConnectionStarts,
                TransactionStarts);
        }
    }
}
