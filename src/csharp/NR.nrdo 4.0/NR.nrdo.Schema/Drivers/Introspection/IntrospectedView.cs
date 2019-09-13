using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedView : IntrospectedSchemaObject
    {
        private readonly string body;
        public string Body { get { return body; } }

        public IntrospectedView(string schema, string name, string body)
            : base(schema, name)
        {
            this.body = body;
        }
    }
}
