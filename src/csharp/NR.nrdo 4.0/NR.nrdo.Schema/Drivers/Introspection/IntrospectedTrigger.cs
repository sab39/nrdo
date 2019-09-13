using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedTrigger : IntrospectedSchemaObject
    {
        private readonly IntrospectedTable table;
        public IntrospectedTable Table { get { return table; } }

        private readonly TriggerTiming timing;
        public TriggerTiming Timing { get { return timing; } }

        private readonly TriggerEvents events;
        public TriggerEvents Events { get { return events; } }

        private readonly string body;
        public string Body { get { return body; } }

        public IntrospectedTrigger(string schema, string name, IntrospectedTable table, TriggerTiming timing, TriggerEvents events, string body)
            : base(schema, name)
        {
            this.table = table;
            this.timing = timing;
            this.events = events;
            this.body = body;
        }
    }
}
