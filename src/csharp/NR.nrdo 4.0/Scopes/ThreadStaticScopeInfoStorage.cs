using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Scopes
{
    public class ThreadStaticScopeInfoStorage : IScopeInfoStorage
    {
        [ThreadStatic]
        private static ScopeInfo current;

        public ScopeInfo GetCurrentScopeInfo()
        {
            if (current == null) current = new ScopeInfo();
            return current;
        }
    }
}
