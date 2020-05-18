using NR.nrdo.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NR.nrdo.Stats
{
    public static class NrdoStats
    {
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

        public static int RebalanceIntervalQueries { get { return cachePulseIntervalQueries.Value; } }
        public static TimeSpan RebalanceIntervalTime { get { return cachePulseIntervalTime.Value; } }
        public static int CachePulseIntervalQueries { get { return cachePulseIntervalQueries.Value; } }
        public static TimeSpan CachePulsePulseIntervalTime { get { return cachePulseIntervalTime.Value; } }
        public static int ShrinkCycleCount { get { return cacheCycleIntervalPulses.Value; } }
        public static int CacheCycleIntervalPulses { get { return cacheCycleIntervalPulses.Value; } }
        public static TimeSpan CacheGrowThreshold { get { return cacheGrowThreshold.Value; } }

        public static DateTime GlobalStartTime { get; } = DateTime.Now;

        private static GlobalStats globalStats = new GlobalStats();
        public static GlobalStats GlobalStats { get { return globalStats; } }

        internal static void UpdateGlobalStats(Func<GlobalStats, GlobalStats> updateOp)
        {
            lock (Nrdo.LockObj)
            {
                globalStats = updateOp(globalStats);
            }
        }

        public static long TotalQueries { get { return GlobalStats.TotalQueries; } }

        private static GlobalStats lastRebalanceStats = new GlobalStats(); // Start with an imaginary rebalance at zero to make the first rebalance happen at the right time
        public static GlobalStats LastRebalanceStats { get { return lastRebalanceStats; } }

        private static readonly Stopwatch rebalanceStopwatch = Stopwatch.StartNew();

        private static GlobalStats lastShrinkCycleStats;
        public static GlobalStats LastShrinkCycleStats { get { return lastShrinkCycleStats; } }

        private static int totalPulses;
        public static int TotalPulses { get { return totalPulses; } }

        public static int TotalRebalances { get { return totalPulses; } }
        public static int ShrinkCycleCounter { get { return totalPulses % CacheCycleIntervalPulses; } }
        public static int TotalShrinkCycles { get { return totalPulses / CacheCycleIntervalPulses; } }

        internal static void allowCacheRebalance()
        {
            lock (Nrdo.LockObj)
            {
                if (TotalQueries >= lastRebalanceStats.TotalQueries + RebalanceIntervalQueries &&
                    rebalanceStopwatch.Elapsed >= RebalanceIntervalTime)
                {
                    rebalance();
                }
            }
        }
        private static void rebalance()
        {
            // Accumulate cost for all caches
            long cost = 0;
            foreach (var hitInfo in Nrdo.GetCacheHitInfoUnsorted())
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

            rebalanceStopwatch.Restart();
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
            var growCandidates = (from hitInfo in Nrdo.GetCacheHitInfoUnsorted()
                                  let stats = hitInfo.CacheStats.ToNow()
                                  let sinceGrow = stats.Since(hitInfo.lastGrownStats)
                                  let gainSinceGrow = sinceGrow.PotentialImpactGainHybrid
                                  where gainSinceGrow > TimeSpan.Zero
                                  let ratioSinceGrow = Portion.SafeRatio(gainSinceGrow, sinceGrow.LatestGlobalStats.TotalQueryTime)

                                  let thisCycle = hitInfo.lastShrinkCycleStats == null
                                      || (hitInfo.lastGrownStats != null && hitInfo.lastGrownStats.LatestGlobalStats.LatestOperationStamp >= hitInfo.lastShrinkCycleStats.LatestGlobalStats.LatestOperationStamp)
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
                    candidate.hitInfo.Cache.Capacity = Math.Min(targetCapacity, Nrdo.MaxCacheCapacity);
                }
                else
                {
                    var listSinceGrow = candidate.sinceGrow.ListStats;
                    var listCache = (IListCache)candidate.hitInfo.Cache;

                    //  - For list caches,
                    //    - Calculate PotentialCost: Max(PeakItemCount, ItemCapacity, Capacity * WeightedResultItems)
                    var potentialCost = Math.Max(Math.Max(listCache.ItemCapacity, listCache.PeakItemCount), listCache.Capacity * listSinceGrow.WeightedResultItems);

                    //    - Calculate TargetCost = GrowthFactor * Max(PotentialCost, WeightedResultItems)
                    var targetCost = Math.Min(growthFactor * Math.Max(potentialCost, candidate.hitInfo.CacheStats.ListStats.WeightedResultItems), Nrdo.MaxCacheCapacity);

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
            var caches = from cache in Nrdo.GetCacheHitInfoUnsorted()
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
                if (cumulativeQueryTime + cumulativeQueryTime >= globalQueryTime) return cache.impact;
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

            var costs = from cache in Nrdo.GetCacheHitInfoUnsorted()
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
                                    where hitInfo.Cache.IsEnabled && hitInfo.lastShrinkCycleStats != null && (hitInfo.lastGrownStats == null || hitInfo.lastGrownStats.LatestGlobalStats.LatestOperationStamp < hitInfo.lastShrinkCycleStats.LatestGlobalStats.LatestOperationStamp)
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

        private static readonly TimeSpan minTimeInterval = TimeSpan.FromTicks(100); // 10 microseconds
        internal static TimeSpan nonZeroTime(TimeSpan span)
        {
            return span > minTimeInterval ? span : minTimeInterval;
        }
    }
}
