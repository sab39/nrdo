using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Tool;
using System;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Tables
{
    public sealed class DropTablesStep : StepBase
    {
        public override string Identifier { get { return "dropping-tables"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AddTablesStep || other is DropFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            if (changes.Options.TableDropBehavior == DropBehavior.SkipDrop) return;
            var errorResponse = changes.Options.TableDropBehavior == DropBehavior.TryDrop ? ErrorResponse.Ignore : ErrorResponse.Fail;

            foreach (var current in TableType.AllFrom(changes.Current))
            {
                if (changes.Desired.Contains(current)) continue;

                if (!changes.AllowDropWithPossibleDataLoss(current, changes.Options.TableDropBehavior)) continue;

                // Drop the table
                changes.Remove(changes.SchemaDriver.GetDropTableSql(current.Name), errorResponse, current);
            }
        }
    }
}
