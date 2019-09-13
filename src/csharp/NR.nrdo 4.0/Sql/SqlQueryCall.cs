using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Sql
{
    public class SqlQueryCall<T>
    {
        private readonly SqlQueryWhere whereObject;
        private readonly Func<ISqlResult, T> createResult;

        internal SqlQueryCall(SqlQueryWhere whereObject, Func<ISqlResult, T> createResult)
        {
            this.whereObject = whereObject;
            this.createResult = createResult;
        }

        public List<T> Call()
        {
            return SqlResultRow.get(whereObject).Select(createResult).ToList();
        }
    }
}
