using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class RenameTablesStep : StepBase
    {
        public override string Identifier { get { return "renaming-tables"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is SetNullStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddTablesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            var renames = from rename in TableRenameType.AllFrom(changes.Desired)
                          where changes.Current.ContainsRoot(TableType.Identifier(rename.Name))
                          group rename by rename.State.ToTable into destination
                          select destination;

            foreach (var rename in renames)
            {
                var toTable = rename.Key;

                if (rename.Count() > 1)
                {
                    changes.Fail("Rename conflict: Multiple tables (" + string.Join(", ", from source in rename select source.Name) + ") wish to be renamed to " + toTable.Name);
                    continue;
                }
                var fromTable = TableType.Identifier(rename.Single().Name);

                if (changes.Current.ContainsRoot(toTable))
                {
                    changes.Fail("Rename conflict: " + fromTable + " wishes to be renamed to " + rename.Key.Name + ", but that table already exists.");
                    continue;
                }

                if (changes.Desired.ContainsRoot(fromTable))
                {
                    changes.Fail("Rename conflict: " + fromTable + " wishes to be renamed to " + rename.Key.Name + " but also to continue existing with its current name.");
                    continue;
                }

                if (!changes.Desired.ContainsRoot(toTable))
                {
                    changes.Fail("Rename conflict: " + fromTable + " wishes to be renamed to " + rename.Key.Name + " but that table is not supposed to exist.");
                    continue;
                }

                changes.Rename(changes.SchemaDriver.GetRenameTableSql(fromTable.Name, toTable.Name), fromTable, toTable);
            }
        }
    }
}
