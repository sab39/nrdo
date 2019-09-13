using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Indexes
{
    public sealed class DropIndexesStep : StepBase
    {
        public override string Identifier { get { return "dropping-indexes"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropFkeysStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddIndexesStep || other is AlterFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            // This step covers both unique keys (including primary keys) and non-unique indexes
            foreach (var current in UniqueIndexType.AllFrom(changes.Current))
            {
                if (!UniqueIndexType.IndexHasChanged(changes, current)) continue;

                changes.Remove(changes.SchemaDriver.GetDropUniqueConstraintSql(current.ParentName, current.Name), current);
            }
            foreach (var current in NonUniqueIndexType.AllFrom(changes.Current))
            {
                if (!NonUniqueIndexType.IndexHasChanged(changes, current)) continue;

                changes.Remove(changes.SchemaDriver.GetDropIndexSql(current.ParentName, current.Name), current);
            }
        }
    }
}
