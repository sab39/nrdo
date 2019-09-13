using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo
{
    public interface ITypedMethod<TInterface>
        where TInterface : class, ITableObject
    {
        void Invoke<T>(T item) where T : DBTableObject<T>, TInterface;
    }
    public interface ITypedMethod : ITypedMethod<ITableObject> { }
}
