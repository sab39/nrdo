using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Internal
{
    public class FinishingStep : StepBase
    {
        public override string Identifier { get { return "finishing"; } }

        // Must happen last - after everything!
        public override bool MustHappenAfter(StepBase other)
        {
            return true;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            var completion = CompletionType.AllFrom(changes.Desired).Single();
            if (!changes.Current.Contains(completion))
            {
                changes.Put(null, completion);
            }
        }
    }
}
