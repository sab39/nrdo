using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Stats;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public interface IDBObjectCache : ICache
    {
        CacheHitInfo HitInfo { get; }
        long ModificationCountHash { get; }
        bool IsOverflowing { get; }
        bool IsEnabled { get; }
    }
    public interface IDBObjectCache<T> : IDBObjectCache
        where T : DBObject<T>
    {
    }
}
