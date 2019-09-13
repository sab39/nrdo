using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class DropChangedFieldsStep : StepBase
    {
        public override string Identifier { get { return "dropping-changed-fields"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AlterFieldsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is SetNullStep;
        }

        private bool isSequencedPkeyToBeDropped(SchemaChanges changes, SubObjectState<FieldType, FieldType.State> current)
        {
            if (current.State.IsSequencedPkey)
            {
                var desiredTable = TableType.GetDesiredIdentifier(changes, current.ParentIdentifier);
                return desiredTable != null && FieldType.ChildrenFrom(changes.Desired, desiredTable).Any(field => field.State.IsSequencedPkey);
            }
            return false;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FieldType.AllFrom(changes.Current))
            {
                var desired = FieldType.GetDesired(changes, current);

                // At this point we are only dropping changed fields, not fields that are being
                // removed entirely, which we keep around til later to allow them to be used by
                // before statements for migration. So we only want to drop fields that conflict
                // with fields in the eventual state of the table (including the conflict
                // that would occur if two identity fields existed in the same table).
                if (desired == null && !isSequencedPkeyToBeDropped(changes, current)) continue;

                // If the type and sequenced-ness are unchanged then we don't need to do anything here.
                if (desired != null && FieldType.IsTypeEqual(current.State, desired.State, changes.SchemaDriver.IsSequencedPartOfFieldDeclaration)) continue;

                // Fields being dropped here need to be replaced later, so failing to drop them has to be fatal regardless of FieldDropBehavior
                if (!changes.AllowDropWithPossibleDataLoss(current, DropBehavior.Drop)) continue;

                // If we are about to drop the last field from a table, add a temporary field so that the drop will be allowed
                if (FieldType.ChildrenFrom(changes.Current, current.ParentIdentifier).Count() == 1)
                {
                    var placeholder = FieldType.Create(current.ParentIdentifier, "nrdo_placeholder_field", 2, "int", true);
                    changes.Put(changes.SchemaDriver.GetAddFieldSql(placeholder.ParentName, placeholder.Name, placeholder.State.DataType, false, null), placeholder);
                    if (changes.HasFailed) continue;
                }

                // Drop the field
                changes.Remove(changes.SchemaDriver.GetDropFieldSql(current.ParentName, current.Name), current);
            }
        }
    }
}
