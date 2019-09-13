using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class DropFulltextIndexesStep : StepBase
    {
        public override string Identifier { get { return "dropping-fulltext-indexes"; } }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropFulltextCatalogsStep || other is AddFulltextIndexesStep ||
                other is DropTablesStep || other is AlterFieldsStep || other is DropIndexesStep ||
                other is RenameTablesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FulltextIndexType.AllFrom(changes.Current))
            {
                if (!FulltextIndexType.FulltextIndexHasChanged(changes, current)) continue;

                changes.RemoveWithoutTransaction(changes.SchemaDriver.GetDropFulltextIndexSql(current.Name), current);
            }
        }
    }
}
