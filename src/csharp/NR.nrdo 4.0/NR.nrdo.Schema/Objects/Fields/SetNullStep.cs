using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class SetNullStep : StepBase
    {
        public override string Identifier { get { return "setting-null"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropChangedFieldsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddTablesStep || other is RenameTablesStep || other is AddFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FieldType.AllFrom(changes.Current))
            {
                // Don't need to set null if it's already null!
                if (current.State.IsNullable) continue;

                // Don't need to set null if nullability is already correct
                var desired = FieldType.GetDesired(changes, current);
                if (desired != null && !desired.State.IsNullable) continue;

                // Sequenced pkey fields can't be made nullable
                if (current.State.IsSequencedPkey && !changes.SchemaDriver.IsAlterSequencedFieldSupported) continue;

                // The field is either being dropped (desired == null) or set to nullable
                // Either way we set it to null now
                // We know to specify "false, null" for sequenced key field because the desired state can't be nullable if it's sequenced
                var setNullSql = changes.SchemaDriver.GetSetFieldNullSql(current.ParentName, current.Name, current.State.DataType, false, null);
                changes.Put(setNullSql, current.With(state => state.WithNullable(true)));
            }
        }
    }
}
