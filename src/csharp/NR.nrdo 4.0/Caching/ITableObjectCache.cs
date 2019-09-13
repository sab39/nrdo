using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public interface ITableObjectCache : IDBObjectCache
    {
    }
    public interface ITableObjectCache<T> : ITableObjectCache, IDBObjectCache<T>
        where T : DBTableObject<T>
    {
        void ReactToInsert(T t);
        void ReactToUpdate(T t);
        void ReactToDelete(T t);
    }
}
