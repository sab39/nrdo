using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class TableType : RootObjectType<TableType, Stateless>
    {
        public override string Name { get { return "table"; } }

        public override IEnumerable<RootObjectState<TableType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from table in connection.GetAllTables()
                   select Create(table.QualifiedName);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropTablesStep();
                // Rename tables step is provided by TableRenameType and can be skipped entirely if TableRenameType is not in play
                yield return new AddTablesStep();
            }
        }

        public static RootObjectState<TableType, Stateless> Create(string name)
        {
            return CreateState(name, Stateless.Value);
        }

        public static Identifier GetDesiredIdentifier(SchemaChanges changes, Identifier ident)
        {
            CheckType(ident);

            if (changes.Desired.ContainsRoot(ident)) return ident;

            var rename = TableRenameType.GetFrom(changes.Desired, TableRenameType.Identifier(ident.Name));
            if (rename != null) return rename.State.ToTable;

            return null;
        }
    }
}
