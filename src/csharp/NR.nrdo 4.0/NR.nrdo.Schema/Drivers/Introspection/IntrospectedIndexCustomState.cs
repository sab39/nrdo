using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    // This is an SQL-server specific feature but we don't have a general-purpose solution to allow extensibility for now
    public sealed class IntrospectedIndexCustomState
    {
        private readonly IntrospectedTable table;
        public IntrospectedTable Table { get { return table; } }

        private readonly string indexName;
        public string IndexName { get { return indexName; } }

        private readonly IndexCustomState customState;
        public IndexCustomState CustomState { get { return customState; } }

        public IntrospectedIndexCustomState(IntrospectedTable table, string indexName, IndexCustomState customState)
        {
            this.table = table;
            this.indexName = indexName;
            this.customState = customState;
        }
    }
}
