using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Scopes
{
    public class StaticGlobalScopeInfoStorage : IScopeInfoStorage
    {
        private static ScopeInfo current = new ScopeInfo();

        public ScopeInfo GetCurrentScopeInfo()
        {
            return current;
        }
    }
}
