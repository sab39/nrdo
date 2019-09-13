using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class AddFieldsStep : StepBase
    {
        public override string Identifier { get { return "adding-fields"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is SetNullStep || other is AddTablesStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is SetNotNullStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            var fieldsToBeAdded = from field in FieldType.AllFrom(changes.Desired)
                                  where !changes.Current.Contains(field)
                                  orderby field.State.OrdinalPosition
                                  select field;
            // FIXME It's a little weird to be ordering by OrdinalPosition independent of table, because it means we jump from table to table adding all
            // fifth columns, then all sixth columns, etc, but it doesn't hurt anything.

            foreach (var desired in fieldsToBeAdded)
            {
                // We create all fields as initially nullable...
                var fieldToCreate = desired.With(s => s.WithNullable(true));

                // ... except for sequenced fields on databases that don't support altering sequenced fields to notnull later.
                if (desired.State.IsSequencedPkey && !changes.SchemaDriver.IsAlterSequencedFieldSupported)
                {
                    fieldToCreate = desired;
                }

                // Add the field
                changes.Put(changes.SchemaDriver.GetAddFieldSql(desired.ParentName, desired.Name, desired.State.DataType, desired.State.IsSequencedPkey, desired.State.SequenceName),
                    fieldToCreate);
            }
        }
    }
}
