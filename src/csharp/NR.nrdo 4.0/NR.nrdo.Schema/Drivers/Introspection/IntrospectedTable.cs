using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedTable : IntrospectedSchemaObject
    {
        public IntrospectedTable(string schema, string name)
            : base(schema, name) { }
    }
}
