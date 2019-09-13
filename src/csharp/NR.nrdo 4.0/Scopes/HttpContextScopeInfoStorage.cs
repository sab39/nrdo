using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NR.nrdo.Scopes
{
    public class HttpContextScopeInfoStorage : IScopeInfoStorage
    {
        public ScopeInfo GetCurrentScopeInfo()
        {
            var context = HttpContext.Current;
            if (context.Items["NrdoScopeInfo"] == null)
            {
                context.Items["NrdoScopeInfo"] = new ScopeInfo();
            }
            return (ScopeInfo)context.Items["NrdoScopeInfo"];
        }
    }
}
