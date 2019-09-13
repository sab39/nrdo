using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Indexes
{
    public sealed class AddIndexesStep : StepBase
    {
        public override string Identifier { get { return "adding-indexes"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropIndexesStep || other is SetNotNullStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddFkeysStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            // This step covers both unique keys (including primary keys) and non-unique indexes
            foreach (var desired in UniqueIndexType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                var sql = changes.SchemaDriver.GetCreateUniqueConstraintSql(desired.ParentName, desired.State.IsPrimaryKey, desired.Name,
                    desired.State.IndexState.FieldNames, desired.State.IndexState.CustomState);
                changes.Put(sql, desired);
            }
            foreach (var desired in NonUniqueIndexType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                var sql = changes.SchemaDriver.GetCreateIndexSql(desired.ParentName, desired.Name, desired.State.FieldNames, desired.State.CustomState);
                changes.Put(sql, desired);
            }
        }
    }
}
