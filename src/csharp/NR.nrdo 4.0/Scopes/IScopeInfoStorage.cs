using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Scopes
{
    public interface IScopeInfoStorage
    {
        ScopeInfo GetCurrentScopeInfo();
    }
}
