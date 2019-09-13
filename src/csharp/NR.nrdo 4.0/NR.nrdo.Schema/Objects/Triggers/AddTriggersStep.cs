using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Triggers
{
    public sealed class AddTriggersStep : StepBase
    {
        public override string Identifier { get { return "adding-triggers"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropTriggersStep || other is AddFkeysStep || other is AddSequencesStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddStoredProcsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in TriggerType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                var table = desired.ParentIdentifier;

                changes.Put(changes.SchemaDriver.GetCreateTriggerSql(table.Name, desired.Name, desired.State.Timing, desired.State.Events, desired.State.Body), desired);
            }
        }
    }
}
