using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Fkeys;
using System.Collections.Immutable;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Tables
{
    public class ReorderTableColumnsStep : StepBase
    {
        public override string Identifier { get { return "reordering-table-columns"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AddFieldsStep || other is DropIndexesStep || other is DropFkeysStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is SetNotNullStep || other is AddIndexesStep || other is AddFkeysStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            createTempTables(changes, output);
            dropOriginalTables(changes, output);
            renameTempTables(changes, output);
        }

        private void createTempTables(SchemaChanges changes, IOutput output)
        {
            // For any tables with order-sensitivity and fields in different order between current and desired (ignoring fields that only exist in current),
            // Create a new table  "_nrdo_reorder" using "select (columns in correct order followed by columns to be dropped) from original table into newname"
            // Put the table, the fields (with nullability, datatype and identityness as current but in new order) and a PendingReorderTable into current
            // For any tables that need their fields reordered, create a copy of the table with the fields in the right order.
            foreach (var origTable in TableType.AllFrom(changes.Current))
            {
                // Tables that don't exist in desired state are to be dropped, they certainly don't need to be reordered!
                if (!changes.Desired.Contains(origTable)) continue;

                // Figure out whether a reorder is needed
                if (!FieldType.IsFieldReorderNeeded(changes, origTable.Identifier)) continue;

                // Put the fields in order.
                // At this point there may be extra fields that are to be dropped but haven't been yet. They need to stick around until
                // it's time to drop them, because before statements may rely on them. But we put those ones last (in the order they're currently in).
                var fieldsInOrder = from field in FieldType.ChildrenFrom(changes.Current, origTable.Identifier)
                                    let desired = changes.Desired.Get(field)
                                    orderby
                                        desired != null ? desired.State.OrdinalPosition : int.MaxValue,
                                        field.State.OrdinalPosition
                                    select field;

                // Construct the state representation that will be used for the new table.
                // It inherits all before statements from the original table so that when it's renamed they'll still be there.
                // FIXME: This hardcodes the assumption that CreateTableAsSelect preserves field type, nullability and identity-ness, which may or may not be true on other DBs
                // FIXME: any other aspects of field state that we add support for (Default values, check constraints...) need to get dropped here.
                var tempTable = TableType.Create(origTable.Name + "_nrdo_reorder");
                var pendingReorder = PendingReorderTableType.Create(tempTable.Identifier, origTable.Name);
                var tempTableFields = from fi in fieldsInOrder.Select((field, index) => new { field, index })
                                      select fi.field
                                               .WithParent(tempTable.Identifier)
                                               .With(s => s.WithOrdinalPosition(fi.index));
                var tempTableBefores = from before in BeforeStatementType.ChildrenFrom(changes.Current, origTable.Identifier)
                                       select before.WithParent(tempTable.Identifier);

                // Gather all temporary objects into one list to put them into the database together
                var tempTableObjects =
                    new ObjectState[]
                    {
                        tempTable,
                        pendingReorder
                    }
                    .Concat(tempTableFields)
                    .Concat(tempTableBefores);

                var sql = changes.SchemaDriver.GetCreateTableAsSelectSql(tempTable.Name,
                    string.Join(", ", from field in fieldsInOrder select changes.DbDriver.QuoteIdentifier(field.Name)),
                    changes.DbDriver.QuoteSchemaIdentifier(origTable.Name));

                changes.Put(sql, tempTableObjects);
            }
        }

        private void dropOriginalTables(SchemaChanges changes, IOutput output)
        {
            // For any tables that are the target of existing PendingReorderTables, drop them (without prompting).
            foreach (var pending in PendingReorderTableType.AllFrom(changes.Current))
            {
                var originalTable = TableType.Identifier(pending.Name);
                var tempTable = pending.ParentIdentifier;

                if (!changes.Current.ContainsRoot(originalTable)) continue;

                // FIXME: (Consider sanity checks before doing this - anything from "confirm all the right column names exist" to "confirm the right number of rows exist" to
                // "confirm all the data is identical in all the rows")
                changes.Remove(changes.SchemaDriver.GetDropTableSql(originalTable.Name), changes.Current.GetRoot(originalTable));
            }
        }


        private void renameTempTables(SchemaChanges changes, IOutput output)
        {
            // For any PendingReorderTables where the original table doesn't exist, rename the table to the target name and remove the pending rename.
            foreach (var pending in PendingReorderTableType.AllFrom(changes.Current))
            {
                var originalTable = TableType.Identifier(pending.Name);
                var tempTable = pending.ParentIdentifier;

                if (changes.Current.ContainsRoot(originalTable)) continue;

                changes.Rename(changes.SchemaDriver.GetRenameTableSql(tempTable.Name, originalTable.Name), tempTable, originalTable);
                changes.Remove(null, pending.WithParent(originalTable));
            }
        }
    }
}
