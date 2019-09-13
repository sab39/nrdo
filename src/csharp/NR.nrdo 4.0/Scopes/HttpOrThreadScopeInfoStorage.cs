using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NR.nrdo.Scopes
{
    public class HttpOrThreadScopeInfoStorage : IScopeInfoStorage
    {
        private HttpContextScopeInfoStorage httpStorage = new HttpContextScopeInfoStorage();
        private ThreadStaticScopeInfoStorage threadStorage = new ThreadStaticScopeInfoStorage();

        public ScopeInfo GetCurrentScopeInfo()
        {
            var storage = HttpContext.Current == null ? (IScopeInfoStorage) threadStorage : httpStorage;
            return storage.GetCurrentScopeInfo();
        }
    }
}
