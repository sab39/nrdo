using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Caching
{
    public interface IDBSingleObjectCache<T> : IDBObjectCache<T>
        where T : DBObject<T>
    {
        bool TryGetValue(Where<T> where, out T result);
        void StoreValue(Where<T> where, T result);
    }
}
