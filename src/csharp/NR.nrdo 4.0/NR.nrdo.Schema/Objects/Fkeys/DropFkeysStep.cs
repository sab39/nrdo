using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fkeys
{
    public sealed class DropFkeysStep : StepBase
    {
        public override string Identifier { get { return "dropping-fkeys"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropSequencesStep || other is DropTriggersStep || other is DropStoredProcsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddFkeysStep || other is DropIndexesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FkeyType.AllFrom(changes.Current))
            {
                if (!FkeyType.FkeyHasChanged(changes, current)) continue;

                changes.Remove(changes.SchemaDriver.GetDropFkeySql(current.ParentName, current.Name), current);
            }
        }
    }
}
