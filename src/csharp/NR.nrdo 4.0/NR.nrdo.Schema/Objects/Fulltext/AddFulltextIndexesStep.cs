using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Tables;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class AddFulltextIndexesStep : StepBase
    {
        public override string Identifier { get { return "adding-fulltext-indexes"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AddFulltextCatalogsStep || other is DropFulltextIndexesStep ||
                other is AddFieldsStep || other is AddIndexesStep || other is RenameTablesStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in FulltextIndexType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                changes.PutWithoutTransaction(changes.SchemaDriver.GetCreateFulltextIndexSql(desired.Name, desired.ParentName, desired.State.KeyName, desired.State.Columns),
                    desired);
            }

        }
    }
}
