using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public interface IDBMultiObjectCache<T> : IDBObjectCache<T>, IListCache
        where T : DBObject<T>
    {
        bool TryGetValue(Where<T> where, out List<T> result);
        void StoreValue(Where<T> where, List<T> result);
    }
}
