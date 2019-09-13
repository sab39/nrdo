using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Scopes
{
    public class DatabaseScopeInfo
    {
        internal NrdoScope top;
        internal NrdoScope inner;
        internal NrdoConnection conn;

        internal NrdoTransactedScope topTransacted;
        internal IDbTransaction tx;
    }
}
