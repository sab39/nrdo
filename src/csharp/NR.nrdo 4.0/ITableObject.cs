using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo
{
    public interface ITableObject : IDBObject
    {
        void InvokeTypedMethod<TInterface>(ITypedMethod<TInterface> action)
            where TInterface : class, ITableObject;
        bool IsNew { get; }
        void Update();
        void Delete();
    }
    public interface ITableObject<T> : ITableObject
        where T : DBTableObject<T>
    {
    }
}
