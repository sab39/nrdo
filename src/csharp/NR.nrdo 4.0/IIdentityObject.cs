using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo
{
    public interface IIdentityObject<T, TId> : ITableObject
        where T : DBTableObject<T>, IIdentityObject<T, TId>
    {
        void SetDesiredIdentity(TId id);
    }
}
