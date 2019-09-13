using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class PendingReorderTableType : SubObjectType<PendingReorderTableType, Stateless, TableType, Stateless>
    {
        public override string Name { get { return "pending-reorder-table"; } }

        public override IEnumerable<SubObjectState<PendingReorderTableType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetBasedOnKnownIdentifiers(connection, helper, (parent, name) => Create(parent, name));
        }

        public override IEnumerable<StepBase> Steps
        {
            get { yield return new ResolvePendingReordersStep(); }
        }

        public static SubObjectState<PendingReorderTableType, Stateless> Create(Identifier table, string originalTableName)
        {
            return CreateState(table, originalTableName, Stateless.Value);
        }

        public static bool IsTablePendingReorder(DatabaseState state, Identifier table)
        {
            return table.ObjectType == TableType.Instance && ChildrenFrom(state, table).Any();
        }
    }
}
