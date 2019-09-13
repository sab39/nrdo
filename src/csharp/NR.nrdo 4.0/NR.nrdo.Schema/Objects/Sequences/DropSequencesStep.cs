using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Sequences
{
    public sealed class DropSequencesStep : StepBase
    {
        public override string Identifier { get { return "dropping-seqs"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is AddSequencesStep || other is DropStoredProcsStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropFkeysStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in SequenceType.AllFrom(changes.Current))
            {
                if (changes.Desired.Contains(current)) continue;

                changes.Remove(changes.SchemaDriver.GetDropSequenceSql(current.Name), current);
            }
        }
    }
}
