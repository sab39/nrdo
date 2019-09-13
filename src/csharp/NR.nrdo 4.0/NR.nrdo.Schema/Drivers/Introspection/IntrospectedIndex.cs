using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedIndex
    {
        private readonly IntrospectedTable table;
        public IntrospectedTable Table { get { return table; } }

        private readonly bool isPrimaryKey;
        public bool IsPrimaryKey { get { return isPrimaryKey; } }

        private readonly bool isUnique;
        public bool IsUnique { get { return isUnique; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly ReadOnlyCollection<string> fieldNames;
        public IEnumerable<string> FieldNames { get { return fieldNames; } }

        public IntrospectedIndex(IntrospectedTable table, bool isPrimaryKey, bool isUnique, string name, IEnumerable<string> fieldNames)
        {
            this.table = table;
            this.isPrimaryKey = isPrimaryKey;
            this.isUnique = isUnique;
            this.name = name;
            this.fieldNames = fieldNames.ToList().AsReadOnly();
        }
    }
}
