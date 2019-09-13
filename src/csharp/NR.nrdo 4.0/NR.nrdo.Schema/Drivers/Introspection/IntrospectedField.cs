using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedField
    {
        private readonly IntrospectedTable table;
        public IntrospectedTable Table { get { return table; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly int ordinalPosition;
        public int OrdinalPosition { get { return ordinalPosition; } }

        private readonly string dataType;
        public string DataType { get { return dataType; } }

        private readonly bool isNullable;
        public bool IsNullable { get { return isNullable; } }

        private readonly bool isSequencedPkey;
        public bool IsSequencedPkey { get { return isSequencedPkey; } }

        private readonly string sequenceName;
        public string SequenceName { get { return sequenceName; } }

        public IntrospectedField(IntrospectedTable table, string name, int ordinalPosition, string dataType, bool isNullable, bool isSequencedPkey, string sequenceName)
        {
            this.table = table;
            this.name = name;
            this.ordinalPosition = ordinalPosition;
            this.dataType = dataType;
            this.isNullable = isNullable;
            if (sequenceName != null && !isSequencedPkey) throw new ArgumentException("Can't specify sequence name on a non-sequenced-pkey field");
            this.isSequencedPkey = isSequencedPkey;
            this.sequenceName = sequenceName;
        }
    }
}
