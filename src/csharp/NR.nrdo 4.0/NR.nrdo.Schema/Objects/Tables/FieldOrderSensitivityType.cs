using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class FieldOrderSensitivityType : SubObjectType<FieldOrderSensitivityType, Stateless, TableType, Stateless>
    {
        public override string Name { get { return "field-order-sensitivity"; } }

        public override IEnumerable<SubObjectState<FieldOrderSensitivityType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            // We don't bother storing this in the database - it's a characteristic of the desired state only.
            yield break;
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new ResolvePendingReordersStep();
                yield return new ReorderTableColumnsStep();
            }
        }

        public static SubObjectState<FieldOrderSensitivityType, Stateless> Create(Identifier table)
        {
            // We create with a random name so that two completely unrelated providers can give field order sensitivity to the same table without clashing
            return CreateState(table, Guid.NewGuid().ToString(), Stateless.Value);
        }
    }
}
