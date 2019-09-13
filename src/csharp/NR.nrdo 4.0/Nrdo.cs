using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Collections;
using NR.nrdo.Util;
using NR.nrdo.Caching;
using System.Linq;

namespace NR.nrdo
{
    public static class Nrdo
    {
        private static Lazy<bool> cachingEnabled =
            new Lazy<bool>(() => Nbool.Parse(ConfigurationManager.AppSettings["NrdoCacheEnabled"]) ?? false);
        private static Lazy<int> maxCacheCapacity =
            new Lazy<int>(() => Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheMaxCapacity"]) ?? 100000);
        private static Lazy<int> cachePulseIntervalQueries =
            new Lazy<int>(() =>
                Nint.Parse(ConfigurationManager.AppSettings["NrdoCachePulseIntervalQueries"]) ??
                Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheRebalanceIntervalQueries"]) ??
                1000);
        private static Lazy<TimeSpan> cachePulseIntervalTime =
            new Lazy<TimeSpan>(() => TimeSpan.FromSeconds(
                Nint.Parse(ConfigurationManager.AppSettings["NrdoCachePulseIntervalSeconds"]) ??
                Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheRebalanceIntervalSeconds"]) ??
                10));
        private static Lazy<int> cacheCycleIntervalPulses =
            new Lazy<int>(() => Math.Max(10, Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheCycleIntervalPulses"]) ?? 100));
        private static Lazy<TimeSpan> cacheGrowThreshold =
            new Lazy<TimeSpan>(() => TimeSpan.FromSeconds(Math.Max(1, Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheGrowThresholdSeconds"]) ?? 10)));

        public static int MaxCacheCapacity { get { return maxCacheCapacity.Value; } }
        public static int RebalanceIntervalQueries { get { return cachePulseIntervalQueries.Value; } }
        public static TimeSpan RebalanceIntervalTime { get { return cachePulseIntervalTime.Value; } }
        public static int CachePulseIntervalQueries { get { return cachePulseIntervalQueries.Value; } }
        public static TimeSpan CachePulsePulseIntervalTime { get { return cachePulseIntervalTime.Value; } }
        public static int ShrinkCycleCount { get { return cacheCycleIntervalPulses.Value; } }
        public static int CacheCycleIntervalPulses { get { return cacheCycleIntervalPulses.Value; } }
        public static TimeSpan CacheGrowThreshold { get { return cacheGrowThreshold.Value; } }

        public static bool CachingEnabled
        {
            get { return cachingEnabled.Value; }
            set { cachingEnabled = new Lazy<bool>(() => value); }
        }

        internal static void RegisterCache<T, TWhere, TCache>(TCache cache)
            where T : DBObject<T>
            where TWhere : CachingWhereBase<T, TWhere, TCache>
            where TCache : DBObjectCacheBase<T, TWhere, TCache>
        {
            lock (LockObj)
            {
                caches.Add(cache);
            }
        }
        private static List<IDBObjectCache> caches = new List<IDBObjectCache>();

        public static event Action FullCacheFlush;

        public static void FlushCache()
        {
            lock (LockObj)
            {
                var flush = FullCacheFlush;
                if (flush != null) flush();
            }
        }

        public static IEnumerable<CacheHitInfo> GetCacheHitInfo()
        {
            lock (LockObj)
            {
                return (from cache in caches
                        orderby cache.HitInfo
                        select cache.HitInfo).ToList();
            }
        }

        public static IEnumerable<CacheHitInfo> GetCacheHitInfoUnsorted()
        {
            lock (LockObj)
            {
                return (from cache in caches
                        where cache.IsEnabled
                        select cache.HitInfo).ToList();
            }
        }

        public static int CountCaches()
        {
            lock (LockObj) { return caches.Count; }
        }

        private static event DebugPrinter oldDebug;
        public static event DebugPrinter Debug
        {
            add
            {
                if (oldDebug == null) DebugMessage += fireOldDebug;
                oldDebug += value;
            }
            remove
            {
                oldDebug -= value;
                if (oldDebug == null) DebugMessage -= fireOldDebug;
            }
        }
        private static void fireOldDebug(object sender, DebugEventArgs e)
        {
            oldDebug(e.TimeStamp.ToString("hh:mm:ss") + ": " + e.EventType + " - " + e.ClassName + "." + e.MethodName + "(" + e.ParameterString + ")");
        }
        public static event EventHandler<DebugEventArgs> DebugMessage;

        public static void DebugLog(string eventType, string className, string methodName, string parameterString)
        {
            DebugLog(null, eventType, className, methodName, parameterString);
        }
        public static void DebugLog(DateTime? startTimeStamp, string eventType, string className, string methodName, string parameterString)
        {
            var prt = DebugMessage;
            var now = DateTime.Now;
            if (prt != null) prt(null, new DebugEventArgs
            {
                StartTimeStamp = startTimeStamp ?? now,
                TimeStamp = now,
                EventType = eventType,
                ClassName = className,
                MethodName = methodName,
                ParameterString = parameterString,
            });
        }
        internal static bool Debugging { get { return DebugMessage != null; } }

        private static readonly object lockObj = new object();
        public static object LockObj { get { return lockObj; } }

        public static List<T> GetMulti<T>(Where<T> where)
            where T : DBTableObject<T>
        {
            return DBTableObject<T>.getMulti(where);
        }

        public static string GetSelectSql<T>() where T : DBTableObject<T>
        {
            return DBTableObject<T>.selectStatement;
        }

        private static GlobalStats globalStats = new GlobalStats();
        public static GlobalStats GlobalStats { get { return globalStats; } }

        internal static void UpdateGlobalStats(Func<GlobalStats, GlobalStats> updateOp)
        {
            lock (LockObj)
            {
                globalStats = updateOp(globalStats);
            }
        }

        public static long TotalQueries { get { return GlobalStats.TotalQueries; } }

        private static GlobalStats lastRebalanceStats = new GlobalStats(); // Start with an imaginary rebalance at zero to make the first rebalance happen at the right time
        public static GlobalStats LastRebalanceStats { get { return lastRebalanceStats; } }

        private static GlobalStats lastShrinkCycleStats;
        public static GlobalStats LastShrinkCycleStats { get { return lastShrinkCycleStats; } }

        private static int totalPulses;
        public static int TotalPulses { get { return totalPulses; } }

        public static int TotalRebalances { get { return totalPulses; } }
        public static int ShrinkCycleCounter { get { return totalPulses % CacheCycleIntervalPulses; } }
        public static int TotalShrinkCycles { get { return totalPulses / CacheCycleIntervalPulses; } }

        internal static void allowCacheRebalance()
        {
            lock (LockObj)
            {
                if (TotalQueries >= lastRebalanceStats.TotalQueries + RebalanceIntervalQueries &&
                    DateTime.Now >= lastRebalanceStats.LatestOperationTime + RebalanceIntervalTime)
                {
                    rebalance();
                }
            }
        }
        private static void rebalance()
        {
            // Accumulate cost for all caches
            long cost = 0;
            foreach (var hitInfo in GetCacheHitInfoUnsorted())
            {
                cost += hitInfo.CycleCost();
            }
            globalStats = globalStats.WithCycleCost(cost);

            // Do the grow cycle
            totalPulses++;
            doGrowPulse();

            // See if it's time for a shrink cycle
            if (ShrinkCycleCounter == 0)
            {
                doShrinkCycle();
            }

            // Store last rebalance info
            lastRebalanceStats = globalStats.ToNow();
        }

        private static void doGrowPulse()
        {
            // - Grow caches where ImpactGainHybrid is "high" both as a percentage of total DB time and in
            //   absolute value as measured over the period *since this cache was last rebalanced*.
            //   Algorithm:
            //   - Take all caches where ImpactGainHybrid[since last grown] is greater than zero
            //     - Sort by ImpactGainHybrid[since last grown]/Nrdo.TotalQueryTime[since last grown]
            //   - Take all caches that existed at last cycle, haven't grown since then, and where ImpactGainHybrid[this cycle] is greater than zero
            //     (using this pulse would be better but we don't have this cache's stats for that)
            //     - Sort by ImpactGainHybrid[this cycle]/Nrdo.TotalQueryTime[this cycle]
            //   - Grow any caches that are in the top 50% of either list and also have ImpactGainHybrid[since last grown] > CacheGrowThreshold
            var growCandidates = (from hitInfo in GetCacheHitInfoUnsorted()
                                  let stats = hitInfo.CacheStats.ToNow()
                                  let sinceGrow = stats.Since(hitInfo.lastGrownStats)
                                  let gainSinceGrow = sinceGrow.PotentialImpactGainHybrid
                                  where gainSinceGrow > TimeSpan.Zero
                                  let ratioSinceGrow = Portion.SafeRatio(gainSinceGrow, sinceGrow.LatestGlobalStats.TotalQueryTime)

                                  let thisCycle = hitInfo.lastShrinkCycleStats == null
                                      || (hitInfo.lastGrownStats != null && hitInfo.lastGrownStats.LatestGlobalStats.LatestOperationTime >= hitInfo.lastShrinkCycleStats.LatestGlobalStats.LatestOperationTime)
                                      ? null : stats.Since(hitInfo.lastShrinkCycleStats)
                                  let gainThisCycle = thisCycle == null ? TimeSpan.Zero : thisCycle.PotentialImpactGainHybrid
                                  let ratioThisCycle = thisCycle == null ? Portion.Zero : Portion.SafeRatio(gainThisCycle, thisCycle.LatestGlobalStats.TotalQueryTime)
                                  select new
                                  {
                                      hitInfo,
                                      stats,
                                      sinceGrow,
                                      gainSinceGrow,
                                      ratioSinceGrow,
                                      thisCycle,
                                      gainThisCycle,
                                      ratioThisCycle
                                  }).ToList();

            if (!growCandidates.Any()) return;

            var sinceGrowRatioLimit = growCandidates.OrderBy(c => c.ratioSinceGrow).ElementAt(growCandidates.Count / 2).ratioSinceGrow;

            var cycleCandidates = growCandidates.Where(c => c.gainThisCycle > TimeSpan.Zero).ToList();
            var cycleRatioLimit = cycleCandidates.Any() ? cycleCandidates.OrderBy(c => c.ratioThisCycle).ElementAt(cycleCandidates.Count / 2).ratioThisCycle : Portion.Complete;

            var growCaches = from cache in growCandidates
                             where cache.gainSinceGrow > CacheGrowThreshold &&
                                (cache.ratioSinceGrow >= sinceGrowRatioLimit ||
                                    (cache.gainThisCycle > TimeSpan.Zero && cache.ratioThisCycle >= cycleRatioLimit))
                             select cache;

            foreach (var candidate in growCaches)
            {
                //  - Calculate GrowthFactor: 1 + Max(NonHitsOverCapacity / TotalQueries, 0.2)
                var growthFactor = 1 + Math.Max((double)candidate.sinceGrow.NonHitsOverCapacity / candidate.sinceGrow.TotalQueries, 0.2);

                //  - For non-list caches, Capacity = Max(Capacity * GrowthFactor, Capacity + 2)
                if (!candidate.hitInfo.IsList)
                {
                    var targetCapacity = Math.Max((int)(candidate.hitInfo.Cache.Capacity * growthFactor), candidate.hitInfo.Cache.Capacity + 2);
                    candidate.hitInfo.Cache.Capacity = Math.Min(targetCapacity, MaxCacheCapacity);
                }
                else
                {
                    var listSinceGrow = candidate.sinceGrow.ListStats;
                    var listCache = (IListCache)candidate.hitInfo.Cache;

                    //  - For list caches,
                    //    - Calculate PotentialCost: Max(PeakItemCount, ItemCapacity, Capacity * WeightedResultItems)
                    var potentialCost = Math.Max(Math.Max(listCache.ItemCapacity, listCache.PeakItemCount), listCache.Capacity * listSinceGrow.WeightedResultItems);

                    //    - Calculate TargetCost = GrowthFactor * Max(PotentialCost, WeightedResultItems)
                    var targetCost = Math.Min(growthFactor * Math.Max(potentialCost, candidate.hitInfo.CacheStats.ListStats.WeightedResultItems), MaxCacheCapacity);

                    //    - Calculate TargetCapacity = Max(TargetCost / WeightedResultItems, Capacity + 1)
                    var targetCapacity = Math.Max((int)(targetCost / candidate.hitInfo.CacheStats.ListStats.WeightedResultItems), listCache.Capacity + 1);

                    //      - If ItemCapacity < TargetCapacity * AvgResultItems or (Capacity = 0 and Skipped = 0)
                    if (listCache.ItemCapacity < targetCapacity * candidate.hitInfo.CacheStats.ListStats.AverageResultItems ||
                        (listCache.Capacity == 0 && listSinceGrow.Skipped == 0))
                    {
                        //        - Set ItemCapacity = Max(TargetCost, ItemCapacity + 2)
                        listCache.ItemCapacity = Math.Max((int)targetCost, listCache.ItemCapacity + 2);
                    }
                    else
                    {
                        //        - Set Capacity = TargetCapacity and ItemCapacity = TargetCapacity * AvgResultItems
                        listCache.Capacity = targetCapacity;
                        listCache.ItemCapacity = (int)(targetCapacity * candidate.hitInfo.CacheStats.ListStats.AverageResultItems);
                    }
                }
                candidate.hitInfo.growCount++;
                candidate.hitInfo.lastGrownStats = candidate.hitInfo.CacheStats.ToNow();
            }
        }

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

        public class CacheBaselines
        {
            private readonly CacheBaseline baseline;
            public CacheBaseline Baseline { get { return baseline; } }

            private readonly CacheBaseline cycleBaseline;
            public CacheBaseline CycleBaseline { get { return cycleBaseline; } }

            internal CacheBaselines(CacheBaseline baseline, CacheBaseline cycleBaseline)
            {
                this.baseline = baseline;
                this.cycleBaseline = cycleBaseline;
            }
        }

        public static CacheBaselines CalculateBaselines()
        {
            lock (Nrdo.LockObj)
            {
                var global = Nrdo.GlobalStats;

                var baseline = new CacheBaseline(getCostBaseline(global.CumulativeCost, c => c.CacheStats), Nrdo.TotalPulses,
                                                 getImpactBaseline(global.TotalQueryTime, c => c.CacheStats), global.TotalQueries);

                var lastCycle = Nrdo.LastShrinkCycleStats;
                CacheBaseline cycleBaseline = null;
                if (lastCycle != null)
                {
                    var thisCycle = global.Since(lastCycle);

                    var cyclePulses = Nrdo.ShrinkCycleCounter;
                    if (cyclePulses == 0) cyclePulses = Nrdo.CacheCycleIntervalPulses;

                    cycleBaseline = new CacheBaseline(getCostBaseline(thisCycle.CumulativeCost, c => c.CacheStats.Since(c.LastShrinkCycleStats)), cyclePulses,
                                                      getImpactBaseline(thisCycle.TotalQueryTime, c => c.CacheStats.Since(c.LastShrinkCycleStats)), thisCycle.TotalQueries);
                }

                return new CacheBaselines(baseline, cycleBaseline);
            }
        }

        private static TimeSpan getImpactBaseline(TimeSpan globalQueryTime, Func<CacheHitInfo, CacheStats> getStats)
        {
            var cumulativeQueryTime = TimeSpan.Zero;
            var caches = from cache in GetCacheHitInfoUnsorted()
                         let stats = getStats(cache)
                         let impact = stats.Impact
                         orderby impact ascending
                         select new { impact, totalQueryTime = stats.CumulativeTime };

            foreach (var cache in caches)
            {
                if (cache.impact == TimeSpan.Zero)
                {
                    globalQueryTime -= cache.totalQueryTime; // Caches with zero impact aren't counted as part of the total at all
                }
                else
                {
                    cumulativeQueryTime += cache.totalQueryTime;
                }
                if (cumulativeQueryTime + cumulativeQueryTime  >= globalQueryTime) return cache.impact;
            }

            // If no caches have any impact at all then the baseline is kind of undefined, so we arbitrarily pick the total query time of the entire site as the baseline.
            return Nrdo.GlobalStats.TotalQueryTime;
        }

        private static Portion getCostBaseline(long globalCost, Func<CacheHitInfo, CacheStats> getStats)
        {
            var cumulativeCost = 0L;
            var cacheCount = 0;
            var _90pct = Portion.Ratio(9, 10);
            var threshold = globalCost * _90pct;

            var costs = from cache in GetCacheHitInfoUnsorted()
                        let cost = getStats(cache).CumulativeCost
                        orderby cost descending
                        select cost;

            foreach (var cost in costs)
            {
                cacheCount++;
                cumulativeCost += cost;
                if (cumulativeCost >= threshold)
                {
                    return Portion.Ratio(cumulativeCost, cacheCount);
                }
            }

            // The only way we should be able to get to here is if there aren't any caches at all, but just in case, we define the threshold as equal to 90% of
            // the global cost in that case.
            return threshold;
        }
        
        private static void doShrinkCycle()
        {
            var baselines = CalculateBaselines();

            // sort all caches that have existed and not been grown for a full shrink cycle by score ascending
            // score = (1 + impact/baselineImpact) / (1 + cost/baselineCost)
            // calculate forever and this cycle and take the higher (better) answer
            var shrinkCandidates = (from hitInfo in Nrdo.GetCacheHitInfoUnsorted()
                                    where hitInfo.Cache.IsEnabled && hitInfo.lastShrinkCycleStats != null && (hitInfo.lastGrownStats == null || hitInfo.lastGrownStats.LatestGlobalStats.LatestOperationTime < hitInfo.lastShrinkCycleStats.LatestGlobalStats.LatestOperationTime)
                                    let stats = hitInfo.CacheStats.ToNow()
                                    let thisCycle = stats.Since(hitInfo.lastShrinkCycleStats)
                                    let sinceLastShrink = stats.Since(hitInfo.lastShrunkStats)
                                    let globalScore = (1 + Portion.SafeRatio(stats.Impact, baselines.Baseline.Impact)) / (1 + sinceLastShrink.CumulativeCost / baselines.Baseline.Cost)
                                    let cycleScore = (1 + Portion.SafeRatio(thisCycle.Impact, baselines.CycleBaseline.Impact)) - (1 + thisCycle.CumulativeCost / baselines.CycleBaseline.Cost)
                                    let score = Portion.Max(globalScore, cycleScore)
                                    orderby score ascending
                                    select new { hitInfo, score }).ToList();

            // shrink the first 1/4 of the caches in that list as long as their score is strictly < 100%
            shrinkCandidates = shrinkCandidates.Take(shrinkCandidates.Count / 4).Where(c => c.score < Portion.Complete).ToList();

            foreach (var candidate in shrinkCandidates)
            {
                if (candidate.hitInfo.IsList)
                {
                    var listCache = (IListCache)candidate.hitInfo.Cache;
                    if (listCache.Capacity == 0 && listCache.ItemCapacity == 1)
                    {
                        listCache.Clear();
                    }
                    else if (listCache.ItemCapacity == 0 && listCache.Capacity == 1)
                    {
                        listCache.ItemCapacity = (int)candidate.hitInfo.CacheStats.ListStats.AverageResultItems + 1;
                        listCache.Capacity = 0;
                    }
                    else
                    {
                        listCache.Capacity = listCache.Capacity * 3 / 4;
                        listCache.ItemCapacity = listCache.ItemCapacity * 3 / 4;
                    }
                }
                else
                {
                    if (candidate.hitInfo.Cache.Capacity <= 1)
                    {
                        candidate.hitInfo.Cache.Clear();
                    }
                    else
                    {
                        candidate.hitInfo.Cache.Capacity = candidate.hitInfo.Cache.Capacity * 3 / 4;
                    }
                }
                candidate.hitInfo.shrinkCount++;
                candidate.hitInfo.lastShrunkStats = candidate.hitInfo.CacheStats.ToNow();
            }

            // store last shrink cycle info for each individual cache
            lastShrinkCycleStats = globalStats.ToNow();
            foreach (var hitInfo in Nrdo.GetCacheHitInfoUnsorted())
            {
                hitInfo.lastShrinkCycleStats = hitInfo.CacheStats.ToNow();
            }
        }

        internal static TimeSpan nonZeroTime(TimeSpan span)
        {
            return span > TimeSpan.FromMilliseconds(1) ? span : TimeSpan.FromMilliseconds(1);
        }
    }

    public sealed class GlobalStats
    {
        public GlobalStats()
        {
            this.startTime = DateTime.Now;
            this.latestOperationTime = this.startTime;
        }

        private GlobalStats(DateTime startTime, DateTime latestOperationTime, long cacheHitsTotal, long cacheMissesTotal, long cacheSkippedTotal, long totalModifications, TimeSpan totalQueryTime, TimeSpan totalModificationTime, long cumulativeCost, long scopeStarts, long connectionStarts, long transactionStarts)
        {
            this.startTime = startTime;
            this.latestOperationTime = latestOperationTime;
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

        private readonly DateTime startTime;
        public DateTime StartTime { get { return startTime; } }

        private readonly DateTime latestOperationTime;
        public DateTime LatestOperationTime { get { return latestOperationTime; } }

        // Change all these to be nonstatic, nonlocked and readonly
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
            return new GlobalStats(startTime, DateTime.Now,
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
            return new GlobalStats(startTime, DateTime.Now,
                cacheHitsTotal,
                cacheMissesTotal + 1, // New value
                cacheSkippedTotal,
                totalModifications,
                totalQueryTime + Nrdo.nonZeroTime(queryTime), // New value
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithCacheSkip(TimeSpan queryTime)
        {
            return new GlobalStats(startTime, DateTime.Now,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal + 1, // New value
                totalModifications,
                totalQueryTime + Nrdo.nonZeroTime(queryTime), // New value
                totalModificationTime,
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithModification(TimeSpan modTime)
        {
            return new GlobalStats(startTime, DateTime.Now,
                cacheHitsTotal,
                cacheMissesTotal,
                cacheSkippedTotal,
                totalModifications + 1, // New value
                totalQueryTime,
                totalModificationTime + Nrdo.nonZeroTime(modTime), // New value
                cumulativeCost,
                scopeStarts,
                connectionStarts,
                transactionStarts);
        }

        public GlobalStats WithCycleCost(long cost)
        {
            return new GlobalStats(startTime, DateTime.Now,
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
            return new GlobalStats(startTime, DateTime.Now,
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
            return new GlobalStats(startTime, DateTime.Now,
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
            return new GlobalStats(startTime, DateTime.Now,
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
            return new GlobalStats(prior.latestOperationTime, latestOperationTime,
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
            return new GlobalStats(startTime, DateTime.Now,
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

    public sealed class CacheStats
    {
        public static CacheStats CreateSingle()
        {
            return new CacheStats(DateTime.Now, Nrdo.GlobalStats, null, 0, 0, 0, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }
        public static CacheStats CreateList()
        {
            return new CacheStats(DateTime.Now, Nrdo.GlobalStats, new ListCacheStats(0, 0, 0), 0, 0, 0, TimeSpan.Zero, 0, 0, TimeSpan.Zero);
        }

        private CacheStats(DateTime startTime, GlobalStats latestGlobalStats, ListCacheStats listStats, long hits, long nonHits, long nonHitsOverCapacity, TimeSpan cumulativeTime, long cumulativeCost, long foreverNonHits, TimeSpan foreverCumulativeTime)
        {
            this.startTime = startTime;
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

        private readonly DateTime startTime;
        public DateTime StartTime { get { return startTime; } }

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

            return new CacheStats(startTime, Nrdo.GlobalStats, newListStats,
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

            return new CacheStats(startTime, Nrdo.GlobalStats, newListStats,
                hits,
                nonHits + 1,
                isOverCapacity ? nonHitsOverCapacity + 1 : nonHitsOverCapacity,
                cumulativeTime + Nrdo.nonZeroTime(queryTime),
                cumulativeCost,
                foreverNonHits + 1,
                foreverCumulativeTime + Nrdo.nonZeroTime(queryTime));
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
            return new CacheStats(startTime, latestGlobalStats, listStats,
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

            return new CacheStats(prior.latestGlobalStats.LatestOperationTime, latestGlobalStats,
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
            return new CacheStats(startTime, Nrdo.GlobalStats.ToNow(), listStats,
                hits,
                nonHits,
                nonHitsOverCapacity,
                cumulativeTime,
                cumulativeCost,
                foreverNonHits,
                foreverCumulativeTime);
        }
    }

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

                var grown = lastGrownStats.LatestGlobalStats.LatestOperationTime;
                var shrunk = lastShrunkStats.LatestGlobalStats.LatestOperationTime;
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
                Nrdo.UpdateGlobalStats(globalUpdate);
                cacheStats = statsUpdate(cacheStats);
                Nrdo.allowCacheRebalance();
            }
        }

        // New code should go through CacheStats/ListCacheStats directly, but these old properties need to exist for
        // backward compatibility, and have to be ints rather than longs for the same reason.
        public TimeSpan CumulativeTime { get { return CacheStats.CumulativeTime; } }
        public TimeSpan AverageTime { get { return CacheStats.AverageTime; } }
        public int Hits { get { return (int)CacheStats.Hits; } }
        public int Misses { get { return (int)(IsList ? CacheStats.ListStats.Misses : cacheStats.NonHits); } }
        public int OverflowingMisses { get { return (int)CacheStats.NonHitsOverCapacity; } }
        public int Skipped { get { return IsList ? (int)CacheStats.ListStats.Skipped : 0; } }

        internal int tweakedDirectly;
        public int TweakedDirectly { get { return tweakedDirectly; } }
        internal int iterated;
        public int Iterated { get { return iterated; } }
        internal int tweakedByIteration;
        public int TweakedByIteration { get { return tweakedByIteration; } }

        // These scores are not used by the current algorithm but kept for backward compatibility
        public double InsufficiencyScore
        {
            get { return (double)OverflowingMisses * 1000 * Nrdo.CountCaches() / Nrdo.TotalQueries; }
        }
        public double ExcessivenessScore
        {
            get { return (double)itemCount * Nrdo.TotalQueries / (Nrdo.CountCaches() * (Hits + 1)); }
        }

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
            int result = info.CumulativeTime.CompareTo(CumulativeTime);
            if (result == 0) result = (info.Misses + info.Skipped).CompareTo(Misses + Skipped);
            if (result == 0) result = info.Misses.CompareTo(Misses);
            if (result == 0) result = info.OverflowingMisses.CompareTo(OverflowingMisses);
            if (result == 0) result = info.Hits.CompareTo(Hits);
            if (result == 0) result = Method.CompareTo(info.Method);
            return result;
        }
    }
    public delegate void DebugPrinter(string s);
    public class DebugEventArgs : EventArgs
    {
        public string EventType { get; set; }
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public string ParameterString { get; set; }
        public DateTime StartTimeStamp { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
