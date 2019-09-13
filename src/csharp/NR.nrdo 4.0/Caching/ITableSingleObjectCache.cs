using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util;

namespace NR.nrdo.Caching
{
    public interface ITableSingleObjectCache<T> : IDBSingleObjectCache<T>, ITableObjectCache<T>
        where T : DBTableObject<T>
    {
    }
}
