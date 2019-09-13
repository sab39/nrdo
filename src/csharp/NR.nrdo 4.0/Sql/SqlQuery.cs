using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Sql
{
    public class SqlQuery<T>
    {
        internal SqlQuery(Func<ISqlResult, T> createResult, SqlQueryCache cache)
        {
            this.createResult = createResult;
            this.cache = cache;
        }

        private readonly SqlQueryCache cache;

        private readonly Func<ISqlResult, T> createResult;

        public SqlQueryCall<T> GetCall(params SqlValueParam[] parameters)
        {
            return GetCall((IEnumerable<SqlValueParam>)parameters);
        }

        public SqlQueryCall<T> GetCall(IEnumerable<SqlValueParam> parameters)
        {
            return new SqlQueryCall<T>(cache.getWhereObject(parameters), createResult);
        }

        public List<T> Call(params SqlValueParam[] parameters)
        {
            return GetCall(parameters).Call();
        }

        public List<T> Call(IEnumerable<SqlValueParam> parameters)
        {
            return GetCall(parameters).Call();
        }
    }
}
