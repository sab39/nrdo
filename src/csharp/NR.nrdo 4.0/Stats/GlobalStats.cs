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

        private GlobalStats(TimeSpan startStamp, TimeSpan latestOperationStamp, long cacheHitsTotal, long cacheMissesTotal, long cacheSkippedTotal, long totalModifications, TimeSpan totalQueryTime, TimeSpan totalModificationTime, long cumulativeCost, long scopeStarts, long connectionStarts, long transactionStarts)
        {
            this.StartStamp = startStamp;
            this.LatestOperationStamp = latestOperationStamp;
            this.cacheHitsTotal = cacheHitsTotal;
            this.cacheMissesTotal = cacheMissesTotal;
            this.cacheSkippedTotal = cacheSkippedTotal;
            this.totalModifications = totalModifications;
            this.totalQueryTime = totalQueryTime;
            this.totalModificationTime = totalModificationTime;
            this.cumulativeCost = cumulativeCost;
            this.scopeStarts = scopeStarts;
            this.connectionStarts = connectionStarts;
            this.transactionStarts = transactionStarts;
        }

        public TimeSpan StartStamp { get; }
        public TimeSpan LatestOperationStamp { get; }

        private readonly long cacheHitsTotal;
        public long CacheHitsTotal { get { return cacheHitsTotal; } }

        private readonly long cacheMissesTotal;
        public long CacheMissesTotal { get { return cacheMissesTotal; } }

        private readonly long cacheSkippedTotal;
        public long CacheSkippedTotal { get { return cacheSkippedTotal; } }

        public long TotalQueries { get { return CacheHitsTotal + CacheMissesTotal + CacheSkippedTotal; } }
        public long CacheNonHitsTotal { get { return CacheMissesTotal + CacheSkippedTotal; } }

        private readonly long totalModifications;
        public long TotalModifications { get { return totalModifications; } }

        public long TotalOperations { get { return TotalQueries + TotalModifications; } }

        private readonly TimeSpan totalQueryTime; // DB hits only; cache hits are presumed instantaneous
        public TimeSpan TotalQueryTime { get { return totalQueryTime; } }

        private readonly TimeSpan totalModificationTime;
        public TimeSpan TotalModificationTime { get { return totalModificationTime; } }

        public TimeSpan TotalDBTime { get { return TotalQueryTime + TotalModificationTime; } }

        private readonly long cumulativeCost;
        public long CumulativeCost { get { return cumulativeCost; } }

        private readonly long scopeStarts;
        public long ScopeStarts { get { return scopeStarts; } }

        private readonly long connectionStarts;
        public long ConnectionStarts { get { return connectionStarts; } }

        private readonly long transactionStarts;
        public long TransactionStarts { get { return transactionStarts; } }

        public GlobalStats WithCacheHit()
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal + 1, // New value
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithCacheMiss(TimeSpan queryTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal + 1, // New value
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime + NrdoStats.nonZeroTime(queryTime), // New value
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithCacheSkip(TimeSpan queryTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal + 1, // New value
                totalModifications,
                totalQueryTime + NrdoStats.nonZeroTime(queryTime), // New value
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithModification(TimeSpan modTime)
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications + 1, // New value
                totalQueryTime,
                totalModificationTime + NrdoStats.nonZeroTime(modTime), // New value
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithCycleCost(long cost)
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost + cost, // new value
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithScopeStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost,
                scopeStarts + 1, // new value
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithConnectionStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts + 1, // new value
                transactionStarts);
        }

        public GlobalStats WithTransactionStart()
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts + 1); // new value
        }

        public GlobalStats Since(GlobalStats prior)
        {
            return new GlobalStats(prior.LatestOperationStamp, LatestOperationStamp,
                cacheHitsTotal - prior.cacheHitsTotal,
                cacheMissesTotal - prior.cacheMissesTotal,
                cacheSkippedTotal - prior.cacheSkippedTotal,
                totalModifications - prior.totalModifications,
                totalQueryTime - prior.totalQueryTime,
                totalModificationTime - prior.totalModificationTime,
                cumulativeCost - prior.cumulativeCost,
                scopeStarts - prior.scopeStarts,
                connectionStarts - prior.connectionStarts,
                transactionStarts - prior.transactionStarts);
        }

        public GlobalStats ToNow()
        {
            return new GlobalStats(StartStamp, NowStamp,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime,
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }
    }
}
