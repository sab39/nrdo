using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedFulltextIndex
    {
        private readonly IntrospectedTable table;
        public IntrospectedTable Table { get { return table; } }

        private readonly string catalog;
        public string Catalog { get { return catalog; } }

        private readonly string keyName;
        public string KeyName { get { return keyName; } }

        private readonly ReadOnlyCollection<string> columnNames;
        public IEnumerable<string> ColumnNames { get { return columnNames; } }

        public IntrospectedFulltextIndex(IntrospectedTable table, string catalog, string keyName, IEnumerable<string> columnNames)
        {
            this.table = table;
            this.catalog = catalog;
            this.keyName = keyName;
            this.columnNames = columnNames.ToList().AsReadOnly();
        }
    }
}
