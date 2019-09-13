using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Caching
{
    public abstract class CachingWhereBase<T, TWhere, TCache> : Where<T>
        where T : DBObject<T>
        where TWhere : CachingWhereBase<T, TWhere, TCache>
        where TCache : DBObjectCacheBase<T, TWhere, TCache>
    {
        public override abstract IDBObjectCache<T> Cache { get; }
    }
}