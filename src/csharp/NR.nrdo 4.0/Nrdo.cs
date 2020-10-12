using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Collections;
using NR.nrdo.Util;
using NR.nrdo.Caching;
using System.Linq;
using NR.nrdo.Stats;
using System.Diagnostics;
using System.Collections.Immutable;

namespace NR.nrdo
{
    public static class Nrdo
    {
        private static Lazy<bool> cachingEnabled =
            new Lazy<bool>(() => Nbool.Parse(ConfigurationManager.AppSettings["NrdoCacheEnabled"]) ?? false);
        private static Lazy<int> maxCacheCapacity =
            new Lazy<int>(() => Nint.Parse(ConfigurationManager.AppSettings["NrdoCacheMaxCapacity"]) ?? 100000);

        public static int MaxCacheCapacity { get { return maxCacheCapacity.Value; } }

        public static int RebalanceIntervalQueries => NrdoStats.RebalanceIntervalQueries;
        public static TimeSpan RebalanceIntervalTime => NrdoStats.RebalanceIntervalTime;
        public static int CachePulseIntervalQueries => NrdoStats.CachePulseIntervalQueries;
        public static TimeSpan CachePulsePulseIntervalTime => NrdoStats.CachePulsePulseIntervalTime;
        public static int ShrinkCycleCount => NrdoStats.ShrinkCycleCount;
        public static int CacheCycleIntervalPulses => NrdoStats.CacheCycleIntervalPulses;
        public static TimeSpan CacheGrowThreshold => NrdoStats.CacheGrowThreshold;

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
            caches = caches.Add(cache);
        }
        private static ImmutableList<IDBObjectCache> caches = ImmutableList<IDBObjectCache>.Empty;

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
            // Lock still needed even though 'caches' is an immutable list because the cache objects themselves are mutable so there would be race conditions in the sorting
            lock (LockObj)
            {
                return ImmutableList.CreateRange(from cache in caches
                                                 orderby cache.HitInfo
                                                 select cache.HitInfo);
            }
        }

        public static IEnumerable<CacheHitInfo> GetCacheHitInfoUnsorted()
        {
            // Lock still needed even though 'caches' is an immutable list because the cache objects themselves are mutable so there would be race conditions in the sorting
            lock (LockObj)
            {
                return ImmutableList.CreateRange(from cache in caches
                                                 where cache.IsEnabled
                                                 select cache.HitInfo);
            }
        }

        public static int CountCaches() => caches.Count;

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

        public static void DebugLog(Func<DebugEventArgs> getArgs)
        {
            DebugMessage?.Invoke(null, getArgs());
        }
        public static DebugEventArgs DebugArgs(string eventType, string className, string methodName, string parameterString)
        {
            return DebugArgs((DateTime?)null, eventType, className, methodName, parameterString);
        }
        public static DebugEventArgs DebugArgs(DateTime? startTimeStamp, string eventType, string className, string methodName, string parameterString)
        {
            var now = DateTime.Now;
            return new DebugEventArgs
            {
                StartTimeStamp = startTimeStamp ?? now,
                TimeStamp = now,
                EventType = eventType,
                ClassName = className,
                MethodName = methodName,
                ParameterString = parameterString,
            };
        }
        public static DebugEventArgs DebugArgs(Stopwatch stopwatch, string eventType, string className, string methodName, string parameterString)
        {
            var now = DateTime.Now;
            var start = now;
            if (stopwatch != null) start -= stopwatch.Elapsed;
            return new DebugEventArgs
            {
                StartTimeStamp = start,
                TimeStamp = now,
                EventType = eventType,
                ClassName = className,
                MethodName = methodName,
                ParameterString = parameterString,
            };
        }

        public static void DebugLog(string eventType, string className, string methodName, string parameterString)
        {
            DebugLog(null, eventType, className, methodName, parameterString);
        }
        public static void DebugLog(DateTime? startTimeStamp, string eventType, string className, string methodName, string parameterString)
        {
            DebugLog(() => DebugArgs(startTimeStamp, eventType, className, methodName, parameterString));
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

        public static GlobalStats GlobalStats => NrdoStats.GlobalStats;
        public static long TotalQueries => NrdoStats.TotalQueries;
        public static GlobalStats LastRebalanceStats => NrdoStats.LastRebalanceStats;
        public static GlobalStats LastShrinkCycleStats => NrdoStats.LastShrinkCycleStats;
        public static int TotalPulses => NrdoStats.TotalPulses;
        public static int TotalRebalances => NrdoStats.TotalRebalances;
        public static int ShrinkCycleCounter => NrdoStats.ShrinkCycleCounter;
        public static int TotalShrinkCycles => NrdoStats.TotalShrinkCycles;
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
