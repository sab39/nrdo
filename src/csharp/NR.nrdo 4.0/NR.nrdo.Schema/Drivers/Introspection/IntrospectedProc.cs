using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedProc : IntrospectedSchemaObject
    {
        private readonly ReadOnlyCollection<ProcParam> parameters;
        public IEnumerable<ProcParam> Parameters { get { return parameters; } }

        private readonly string body;
        public string Body { get { return body; } }

        public IntrospectedProc(string schema, string name, IEnumerable<ProcParam> parameters, string body)
            : base(schema, name)
        {
            this.parameters = parameters.ToList().AsReadOnly();
            this.body = body;
        }
    }
}
