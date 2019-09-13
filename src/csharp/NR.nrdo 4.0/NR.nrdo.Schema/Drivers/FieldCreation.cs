using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers
{
    public sealed class FieldCreation
    {
        private readonly string fieldName;
        public string FieldName { get { return fieldName; } }

        private readonly string datatype;
        public string Datatype { get { return datatype; } }

        private readonly bool isNullable;
        public bool IsNullable { get { return isNullable; } }

        private readonly bool isSequencedPkey;
        public bool IsSequencedPkey { get { return isSequencedPkey; } }

        private readonly string sequenceName;
        public string SequenceName { get { return sequenceName; } }

        public FieldCreation(string fieldName, string datatype, bool isNullable, bool isSequencedPkey, string sequenceName)
        {
            this.fieldName = fieldName;
            this.datatype = datatype;
            this.isNullable = isNullable;
            this.isSequencedPkey = isSequencedPkey;
            this.sequenceName = sequenceName;
        }
    }
}
