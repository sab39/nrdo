using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Queries;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class ResolvePendingReordersStep : StepBase
    {
        public override string Identifier { get { return "resolving-pending-reorders"; } }

        public override bool MustHappenBefore(StepBase other)
        {
            // We want to resolve any outstanding reordering operations early, because they can leave tables existing that shouldn't, tables not existing
            // that should, before statements associated with the wrong table, dogs and cats sleeping together, mass hysteria. So we fix this outstanding issue
            // before anything except pre-upgrade-hooks.
            return !(other is PreUpgradeHooksStep);
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var pending in PendingReorderTableType.AllFrom(changes.Current))
            {
                var originalTable = TableType.Identifier(pending.Name);
                var tempTable = pending.ParentIdentifier;

                if (object.Equals(originalTable, tempTable))
                {
                    // If the database fails at precisely the wrong time, the rename can be succeed without dropping the PendingReorder,
                    // so the table thinks it's to be renamed to itself. In that case we need to drop the PendingReorder, NOT drop the table!
                    output.Verbose("Cleaning up after completed reorder of columns in " + originalTable);
                    changes.Remove(null, pending);
                }
                else if (changes.Current.ContainsRoot(originalTable))
                {
                    // If the original table still exists, we drop the temp table so we can start over (in case the data in the table has changed since the temp table
                    // was created, or the reordering has somehow become unnecessary).
                    output.Verbose("Clearing pending reorder of columns in " + originalTable);
                    changes.Remove(changes.SchemaDriver.GetDropTableSql(pending.ParentName), changes.Current.GetRoot(pending.ParentIdentifier));
                }
                else
                {
                    // The original table has been dropped so we need to recreate it by renaming the temp table.
                    output.Verbose("Completing pending reordering of columns in " + originalTable);
                    changes.Rename(changes.SchemaDriver.GetRenameTableSql(tempTable.Name, originalTable.Name), tempTable, originalTable);

                    // And forget about the pending rename.
                    changes.Remove(null, pending.WithParent(originalTable));
                }
            }
        }
    }
}
