using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fkeys
{
    public sealed class AddFkeysStep : StepBase
    {
        public override string Identifier { get { return "adding-fkeys"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropFkeysStep || other is AddIndexesStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddSequencesStep || other is AddTriggersStep || other is AddStoredProcsStep || other is DropFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in FkeyType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                changes.Put(changes.SchemaDriver.GetAddFkeySql(desired.ParentName, desired.State.ToTableName, desired.Name, desired.State.IsCascadeDelete,
                    desired.State.Joins), desired);
            }
        }
    }
}
