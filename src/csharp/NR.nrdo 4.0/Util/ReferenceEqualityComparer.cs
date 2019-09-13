using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace NR.nrdo.Util
{
    public class ReferenceEqualityComparer
    {
        public static ReferenceEqualityComparer<object> Instance { get { return ReferenceEqualityComparer<object>.Instance; } }
    }

    public class ReferenceEqualityComparer<T> : IEqualityComparer<T>, System.Collections.IEqualityComparer
        where T : class
    {
        private static readonly ReferenceEqualityComparer<T> instance = new ReferenceEqualityComparer<T>();
        public static ReferenceEqualityComparer<T> Instance { get { return instance; } }

        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }

        bool System.Collections.IEqualityComparer.Equals(object x, object y)
        {
            // Easier to just accept non-T objects and give the right answer than to try to validate that they're really T
            return object.ReferenceEquals(x, y);
        }

        int System.Collections.IEqualityComparer.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
