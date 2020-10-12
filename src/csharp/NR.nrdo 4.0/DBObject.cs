using System;
using System.Data;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using NR.nrdo.Util;
using System.Reflection;
using NR.nrdo.Caching;
using System.Linq;
using NR.nrdo.Connection;
using NR.nrdo.Stats;
using System.Diagnostics;

namespace NR.nrdo
{
    [Serializable]
    public abstract class DBObject<T> : IDBObject<T>
        where T : DBObject<T>
    {
        protected const int MAX = int.MaxValue;

        internal static Func<NrdoResult, T> createFromResult;
        internal static Func<DataBase> getDataBase;
        internal static Lazy<DataBase> dataBase = new Lazy<DataBase>(() => getDataBase());

        protected internal static DataBase DataBase { get { return dataBase.Value; } }

        protected static bool nrdoInitialize(Func<DataBase> getDataBase, Func<NrdoResult, T> createFromResult)
        {
            DBObject<T>.getDataBase = getDataBase;
            DBObject<T>.createFromResult = createFromResult;
            return true;
        }

        protected static void log(string eventType, Where<T> where)
        {
            log(null, eventType, where);
        }
        protected static void log(Stopwatch stopwatch, string eventType, Where<T> where)
        {
            Nrdo.DebugLog(() => Nrdo.DebugArgs(stopwatch, eventType, typeof(T).FullName, where.GetMethodName.Substring(where.GetMethodName.LastIndexOf('.') + 1), where.GetParameters));
        }

        protected internal virtual T FieldwiseClone()
        {
            // Ideally we shouldn't have a default implementation here at all but for backward compatibility (and .qu files) we need one.
            // Old code was using memberwiseclone anyway so it should be close enough.
            return (T)MemberwiseClone();
        }

        protected static void getVoid(Where<T> where)
        {
            getVoid(DataBase, where);
        }
        protected static void getVoid(DataBase dataBase, Where<T> where)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var scope = new NrdoScope(dataBase))
                {
                    scope.ExecuteSql(where.SQLStatement, where.SetOnCmd, where.CommandType);
                }
            }
            finally
            {
                stopwatch.Stop();
                NrdoStats.UpdateGlobalStats(stats => stats.WithModification(stopwatch.Elapsed));
                log(stopwatch, "db-hit-void", where);
            }
        }

        protected static T getSingle(DataBase dataBase, Where<T> where)
        {
            if (object.Equals(dataBase, DataBase)) return getSingle(where);

            return doGetSingle(dataBase, where);
        }

        protected static T getSingle(Where<T> where)
        {
            var stopwatch = Stopwatch.StartNew();
            bool succeeded = false;
            if (where.Cache == null)
            {
                try
                {
                    var result = doGetSingle(DataBase, where);
                    succeeded = true;
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    NrdoStats.UpdateGlobalStats(stats => succeeded ? stats.WithCacheSkip(stopwatch.Elapsed) : stats.WithFailure(stopwatch.Elapsed));
                }
            }

            var cache = (IDBSingleObjectCache<T>)where.Cache;

            long modCountHash;
            lock (Nrdo.LockObj)
            {
                // Try the cache. If it succeeds, return the result. If it fails, save the cache's ModificationCountHash
                T cacheResult;
                if (cache.TryGetValue(where, out cacheResult))
                {
                    where.Cache.HitInfo.updateStats(stats => stats.WithCacheHit(), stats => stats.WithSingleCacheHit());
                    log("cache-hit-single", where);
                    stopwatch.Stop();
                    return cacheResult == null ? null : cacheResult.FieldwiseClone();
                }
                modCountHash = cache.ModificationCountHash;
            }
            // Hit the DB
            try
            {
                var result = doGetSingle(DataBase, where);
                succeeded = true;
                lock (Nrdo.LockObj)
                {
                    // Check the cache's ModificationCountHash. If it equals the saved value, store the value. Otherwise don't.
                    if (cache.ModificationCountHash == modCountHash)
                    {
                        cache.StoreValue(where, result == null ? null : result.FieldwiseClone());
                    }

                    // Return the result.
                    return result;
                }
            }
            finally
            {
                lock (Nrdo.LockObj)
                {
                    stopwatch.Stop();
                    if (!succeeded)
                    {
                        where.Cache.HitInfo.updateStats(stats => stats.WithFailure(stopwatch.Elapsed), stats => stats.WithFailure(stopwatch.Elapsed));
                    }
                    else
                    {
                        where.Cache.HitInfo.updateStats(stats => stats.WithCacheMiss(stopwatch.Elapsed), stats => stats.WithSingleCacheNonHit(stopwatch.Elapsed, where.Cache.IsOverflowing));
                    }
                }
            }
        }

        protected static bool allowSingleAsFirst { get; set; }

        private static T single(IEnumerable<T> results)
        {
            return allowSingleAsFirst ? results.FirstOrDefault() : results.SingleOrDefault();
        }

        private static T doGetSingle(DataBase dataBase, Where<T> where)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var scope = new NrdoScope(dataBase))
                {
                    return single(scope.ExecuteSql(where.SQLStatement, createFromResult, where.SetOnCmd, where.CommandType));
                }
            }
            finally
            {
                log(stopwatch, "db-hit-single", where);
            }
        }

        protected internal static List<T> getMulti<TWhere>(DataBase dataBase, TWhere where)
            where TWhere : Where<T>
        {
            if (object.Equals(dataBase, DataBase)) return getMulti(where);

            return doGetMulti(dataBase, where);
        }

        protected internal static List<T> getMulti<TWhere>(TWhere where)
            where TWhere : Where<T>
        {
            var stopwatch = Stopwatch.StartNew();

            bool succeeded = false;
            if (where.Cache == null)
            {
                try
                {
                    var result = doGetMulti(DataBase, where);
                    succeeded = true;
                    return result;
                }
                finally
                {
                    stopwatch.Stop();
                    NrdoStats.UpdateGlobalStats(stats => succeeded ? stats.WithCacheSkip(stopwatch.Elapsed) : stats.WithFailure(stopwatch.Elapsed));
                }
            }

            var cache = (IDBMultiObjectCache<T>)where.Cache;

            long modCountHash;
            lock (Nrdo.LockObj)
            {
                // Try the cache. If it succeeds, return the result. If it fails, save the cache's ModificationCountHash
                List<T> cacheResult;
                if (cache.TryGetValue(where, out cacheResult))
                {
                    where.Cache.HitInfo.updateStats(stats => stats.WithCacheHit(), stats => stats.WithListCacheHit(cacheResult.Count));
                    log("cache-hit-multi", where);
                    stopwatch.Stop();
                    return (from t in cacheResult select t.FieldwiseClone()).ToList();
                }
                modCountHash = cache.ModificationCountHash;
            }
            // Hit the DB
            var skipped = false;
            int resultCount = 0;
            try
            {
                var result = doGetMulti(DataBase, where);
                succeeded = true;
                resultCount = result.Count;
                lock (Nrdo.LockObj)
                {
                    // Check the cache's ModificationCountHash. If it equals the saved value, store the value. Otherwise don't.
                    if (cache.ModificationCountHash == modCountHash)
                    {
                        if (cache.Capacity > 0 || result.Count <= cache.ItemCapacity)
                        {
                            cache.StoreValue(where, (from t in result select t.FieldwiseClone()).ToList());
                        }
                        else
                        {
                            skipped = true;
                        }
                    }

                    // Return the result.
                    return result;
                }
            }
            finally
            {
                lock (Nrdo.LockObj)
                {
                    stopwatch.Stop();
                    if (!succeeded)
                    {
                        where.Cache.HitInfo.updateStats(stats => stats.WithFailure(stopwatch.Elapsed), stats => stats.WithFailure(stopwatch.Elapsed));
                    }
                    else if (skipped)
                    {
                        where.Cache.HitInfo.updateStats(stats => stats.WithCacheSkip(stopwatch.Elapsed), stats => stats.WithListCacheSkip(resultCount, stopwatch.Elapsed));
                    }
                    else
                    {
                        where.Cache.HitInfo.updateStats(stats => stats.WithCacheMiss(stopwatch.Elapsed), stats => stats.WithListCacheMiss(resultCount, stopwatch.Elapsed, where.Cache.IsOverflowing));
                    }
                }
            }
        }
        protected static List<T> doGetMulti(DataBase dataBase, Where<T> where)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                using (var scope = new NrdoScope(dataBase))
                {
                    return scope.ExecuteSql(where.SQLStatement, createFromResult, where.SetOnCmd, where.CommandType).ToList();
                }
            }
            finally
            {
                log(stopwatch, "db-hit-multi", where);
            }
        }
    }
}
