using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class SetNotNullStep : StepBase
    {
        public override string Identifier { get { return "setting-notnull"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AddFieldsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropFieldsStep || other is AddIndexesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in FieldType.AllFrom(changes.Desired))
            {
                // We don't need to set it notnull if it's supposed to be nullable
                if (desired.State.IsNullable) continue;

                var current = changes.Current.Get(desired);

                // This shouldn't happen because fields have been created by this point
                if (current == null)
                {
                    changes.Fail("Trying to set " + desired + " not null but it hasn't been added yet!");
                    continue;
                }

                // This shouldn't happen either because field types have been altered by this point
                if (!FieldType.IsTypeEqual(desired.State, current.State, changes.SchemaDriver.IsSequencedPartOfFieldDeclaration))
                {
                    changes.Fail("Trying to set " + desired + " not null but its type is still wrong!");
                    continue;
                }

                // We don't need to set it notnull if it already is
                if (!current.State.IsNullable) continue;

                // Update the field to notnull
                changes.Put(changes.SchemaDriver.GetSetFieldNotNullSql(desired.ParentName, desired.Name, desired.State.DataType, desired.State.IsSequencedPkey, desired.State.SequenceName),
                    desired);
            }
        }
    }
}
