using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Caching
{
    public abstract class CachingWhere<T, TWhere, TCache> : CachingWhereBase<T, TWhere, TCache>
        where T : DBObject<T>
        where TWhere : CachingWhere<T, TWhere, TCache>
        where TCache : DBObjectCacheBase<T, TWhere, TCache>, new()
    {
        private static TCache cache = new TCache();
        public override IDBObjectCache<T> Cache { get { return cache; } }
    }
}
