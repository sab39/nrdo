using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class AddTablesStep : StepBase
    {
        public override string Identifier { get { return "adding-tables"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is SetNullStep || other is RenameTablesStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropTablesStep || other is AddFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            var pendingReorders = PendingReorderTableType.AllFrom(changes.Current);

            foreach (var desired in TableType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                var fields = FieldType.ChildrenFrom(changes.Desired, desired.Identifier);
                var fieldCreation = from field in fields
                                    orderby field.State.OrdinalPosition
                                    select new FieldCreation(field.Name, field.State.DataType, field.State.IsNullable, field.State.IsSequencedPkey, field.State.SequenceName);

                changes.Put(changes.SchemaDriver.GetCreateTableSql(desired.Name, fieldCreation),
                            new ObjectState[] { desired }.Concat(fields));
            }
        }
    }
}
