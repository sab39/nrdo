using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.OldVersionLoader
{
    public abstract class CacheResultCollectorBase : MarshalByRefObject
    {
        public abstract void DidNotFindCacheAttributes(string msg);
        public abstract void Warning(string msg);
        public abstract void StartedAssemblyLoad(string file);
        public abstract void FoundCacheFile(string name, string contents);
    }
}
