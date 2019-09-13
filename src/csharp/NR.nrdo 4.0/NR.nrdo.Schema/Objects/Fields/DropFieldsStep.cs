using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class DropFieldsStep : StepBase
    {
        public override string Identifier { get { return "dropping-fields"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is SetNotNullStep || other is AddFkeysStep || other is AddSequencesStep || other is AddTriggersStep || other is AddStoredProcsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropTablesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            if (changes.Options.FieldDropBehavior == DropBehavior.SkipDrop) return;
            var errorResponse = changes.Options.FieldDropBehavior == DropBehavior.TryDrop ? ErrorResponse.Ignore : ErrorResponse.Fail;

            foreach (var current in FieldType.AllFrom(changes.Current))
            {
                if (changes.Desired.Contains(current)) continue;

                // If we're going to drop the whole table then don't bother dropping the field
                if (!changes.Desired.ContainsRoot(current.ParentIdentifier)) continue;

                if (!changes.AllowDropWithPossibleDataLoss(current, changes.Options.FieldDropBehavior)) continue;

                changes.Remove(changes.SchemaDriver.GetDropFieldSql(current.ParentName, current.Name), current);
            }
        }
    }
}
