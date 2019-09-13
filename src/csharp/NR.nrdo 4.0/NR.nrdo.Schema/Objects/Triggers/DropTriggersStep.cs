using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Triggers
{
    public sealed class DropTriggersStep : StepBase
    {
        public override string Identifier { get { return "dropping-triggers"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropStoredProcsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddTriggersStep || other is DropSequencesStep || other is DropFkeysStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in TriggerType.AllFrom(changes.Current))
            {
                if (!TriggerType.TriggerHasChanged(changes, current)) continue;

                changes.Remove(changes.SchemaDriver.GetDropTriggerSql(current.ParentName, current.Name), current);
            }
        }
    }
}
