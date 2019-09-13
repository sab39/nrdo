using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo
{
    internal abstract class TypedMethodInvoker<T>
        where T : DBTableObject<T>
    {
        internal abstract void Invoke<TInter>(T item, ITypedMethod<TInter> action)
            where TInter : class, ITableObject;

        internal static TypedMethodInvoker<T> Create<TInterface>()
            where TInterface : class, ITableObject
        {
            return (TypedMethodInvoker<T>)Activator.CreateInstance(typeof(TypedMethodInvoker<,>).MakeGenericType(typeof(T), typeof(TInterface)));
        }
    }

    internal interface ITypedMethodInvoker<T, TInterface>
        where T : DBTableObject<T>
        where TInterface : class, ITableObject { }

    internal class TypedMethodInvoker<T, TInterface>
        : TypedMethodInvoker<T>, ITypedMethodInvoker<T, TInterface>
        where T : DBTableObject<T>, TInterface
        where TInterface : class, ITableObject
    {
        internal override void Invoke<TInter>(T item, ITypedMethod<TInter> action)
        {
            ((ITypedMethod<TInterface>)action).Invoke(item);
        }
    }
}
