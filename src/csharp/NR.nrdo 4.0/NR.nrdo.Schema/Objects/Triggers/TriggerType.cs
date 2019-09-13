using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Fields;

namespace NR.nrdo.Schema.Objects.Triggers
{
    public sealed class TriggerType : SubObjectType<TriggerType, TriggerType.State, TableType, Stateless>
    {
        public override string Name { get { return "trigger"; } }

        public override IEnumerable<SubObjectState<TriggerType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from trigger in connection.GetAllTriggers()
                   select Create(TableType.Identifier(trigger.Table.QualifiedName), trigger.QualifiedName, trigger.Timing, trigger.Events, trigger.Body);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropTriggersStep();
                yield return new AddTriggersStep();
            }
        }

        public sealed class State
        {
            private readonly TriggerTiming timing;
            public TriggerTiming Timing { get { return timing; } }

            private readonly TriggerEvents events;
            public TriggerEvents Events { get { return events; } }

            private readonly string body;
            public string Body { get { return body; } }

            internal State(TriggerTiming timing, TriggerEvents events, string body)
            {
                this.timing = timing;
                this.events = events;
                this.body = body;
            }
        }

        public static SubObjectState<TriggerType, State> Create(Identifier table, string name, TriggerTiming timing, TriggerEvents events, string body)
        {
            return CreateState(table, name, new State(timing, events, body));
        }

        public static bool TriggerHasChanged(SchemaChanges changes, SubObjectState<TriggerType, State> current)
        {
            if (current == null) return true;

            var desired = changes.Desired.Get(current);

            // The trigger doesn't exist in the desired schema at all
            if (desired == null) return true;

            // The trigger's contents are different
            if (current.State.Timing != desired.State.Timing ||
                current.State.Events != desired.State.Events ||
                !Nstring.DBEquivalentComparer.Equals(current.State.Body, desired.State.Body)) return true;

            // The table might need to be dropped entirely to reorder the columns
            if (FieldType.IsFieldReorderPossiblyNeeded(changes, current.ParentIdentifier)) return true;

            return false;
        }
    }
}
