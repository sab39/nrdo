using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public abstract class IntrospectedSchemaObject
    {
        private readonly string schema;
        public string Schema { get { return schema; } }

        private readonly string name;
        public string Name { get { return name; } }

        public string QualifiedName { get { return schema + "." + name; } }

        public IntrospectedSchemaObject(string schema, string name)
        {
            this.schema = schema;
            this.name = name;
        }
    }
}
