using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo
{
    public interface IDBObject
    {
    }
    public interface IDBObject<T> : IDBObject
        where T : DBObject<T>
    {
    }
}
