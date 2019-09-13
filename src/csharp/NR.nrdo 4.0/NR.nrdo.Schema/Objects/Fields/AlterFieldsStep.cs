using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class AlterFieldsStep : StepBase
    {
        public override string Identifier { get { return "altering-fields"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropIndexesStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropChangedFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in FieldType.AllFrom(changes.Current))
            {
                var desired = FieldType.GetDesired(changes, current);
                if (desired == null) continue;

                // If we can't alter sequenced keys then no point trying!
                if (!changes.SchemaDriver.IsAlterSequencedFieldSupported &&
                    (current.State.IsSequencedPkey || desired.State.IsSequencedPkey)) continue;

                // If the fields are the same by datatype and sequenced-ness - or the sequenced-ness is irrelevant at the field level -
                // then there's nothing to change
                if (changes.SchemaDriver.DbDriver.StringEquals(current.State.DataType, desired.State.DataType) &&
                    (current.State.IsSequencedPkey == desired.State.IsSequencedPkey || !changes.SchemaDriver.IsSequencedPartOfFieldDeclaration)) continue;

                // Try the alter statement. If it doesn't work, the field will be dropped and added.
                var altered = current.With(s => s.WithTypeChange(desired.State.DataType, desired.State.IsSequencedPkey, desired.State.SequenceName));
                var alterSql = changes.SchemaDriver.GetAlterFieldTypeSql(altered.ParentName, altered.Name, altered.State.DataType, altered.State.IsNullable, altered.State.IsSequencedPkey, altered.State.SequenceName);
                changes.Put(alterSql, ErrorResponse.Ignore, altered);
            }
        }
    }
}