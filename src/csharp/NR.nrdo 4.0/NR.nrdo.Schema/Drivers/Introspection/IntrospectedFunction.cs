using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedFunction : IntrospectedProc
    {
        private readonly string returnType;
        public string ReturnType { get { return returnType; } }

        public IntrospectedFunction(string schema, string name, IEnumerable<ProcParam> parameters, string returnType, string body)
            : base(schema, name, parameters, body)
        {
            this.returnType = returnType;
        }
    }
}
