using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Scopes
{
    public class ScopeInfo
    {
        private static IScopeInfoStorage storage = new HttpOrThreadScopeInfoStorage();
        public static IScopeInfoStorage Storage { get { return storage; } set { storage = value; } }

        public static ScopeInfo Current { get { return Storage.GetCurrentScopeInfo(); } }

        internal Dictionary<DataBase, DatabaseScopeInfo> dbScopes = new Dictionary<DataBase, DatabaseScopeInfo>();

        public DatabaseScopeInfo GetDbScopeInfo(DataBase db)
        {
            if (!dbScopes.ContainsKey(db)) dbScopes[db] = new DatabaseScopeInfo();
            return dbScopes[db];
        }
    }
}
