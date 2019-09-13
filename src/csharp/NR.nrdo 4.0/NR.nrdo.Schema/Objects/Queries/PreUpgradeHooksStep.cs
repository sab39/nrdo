using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Objects.Queries
{
    public sealed class PreUpgradeHooksStep : StepBase
    {
        public override string Identifier { get { return "pre-upgrade-hooks"; } }

        // They're not much of "pre" upgrade hooks if they don't happen first!
        public override bool MustHappenBefore(StepBase other)
        {
            return true;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            // This isn't really anything to do with pre-upgrade-hooks but there's no other good place to give this warning!
            foreach (var query in QueryType.AllFrom(changes.Current))
            {
                if (query.State != null && query.State.Body.StartsWith("<FAIL>"))
                {
                    output.Warning(query.State.Body.Substring(6));
                }
            }

            foreach (var hook in PreUpgradeHookType.AllFrom(changes.Current))
            {
                var proc = QueryType.GetFrom(changes.Current, hook.ParentIdentifier);
                if (proc.State == null || proc.State is QueryType.FunctionState || proc.State.Parameters.Any())
                {
                    output.Warning("Cannot execute pre-upgrade hook " + proc.Name + ": stored procedure does not exist or has parameters.");
                }

                changes.Put(changes.SchemaDriver.GetExecuteProcSql(hook.ParentName));

                if (changes.Desired.Get(hook) == null) changes.Remove(null, hook);
            }
        }
    }
}
