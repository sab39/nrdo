using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class TableRenameType : RootObjectType<TableRenameType, TableRenameType.State>
    {
        public override string Name { get { return "table-rename"; } }

        public override IEnumerable<RootObjectState<TableRenameType, TableRenameType.State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            // Table renames don't get tracked in existing databases at all - no point!
            yield break;
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new RenameTablesStep();
            }
        }

        public struct State
        {
            private readonly string toTableName;
            public string ToTableName { get { return toTableName; } }
            public Identifier ToTable { get { return TableType.Identifier(ToTableName); } }

            internal State(string toTableName)
            {
                this.toTableName = toTableName;
            }
        }

        public static RootObjectState<TableRenameType, State> Create(string fromName, string toName)
        {
            return CreateState(fromName, new State(toName));
        }

        public static bool ContainsRenameTo(DatabaseState state, Identifier toTable)
        {
            return toTable.ObjectType == TableType.Instance && state.ContainsRoot(Identifier(toTable.Name));
        }
    }
}
