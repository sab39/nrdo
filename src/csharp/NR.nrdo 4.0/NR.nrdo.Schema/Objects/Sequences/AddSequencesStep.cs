using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Sequences
{
    public sealed class AddSequencesStep : StepBase
    {
        public override string Identifier { get { return "adding-seqs"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropSequencesStep || other is AddFkeysStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddStoredProcsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in SequenceType.AllFrom(changes.Desired))
            {
                if (changes.Current.Contains(desired)) continue;

                changes.Put(changes.SchemaDriver.GetCreateSequenceSql(desired.Name), desired);
            }
        }
    }
}
