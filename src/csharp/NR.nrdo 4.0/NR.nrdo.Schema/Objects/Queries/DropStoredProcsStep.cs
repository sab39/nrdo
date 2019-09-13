using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Views;

namespace NR.nrdo.Schema.Objects.Queries
{
    public sealed class DropStoredProcsStep : StepBase
    {
        public override string Identifier { get { return "dropping-storedprocs"; } }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is AddStoredProcsStep || other is DropFkeysStep || other is DropViewsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var current in QueryType.AllFrom(changes.Current))
            {
                // Queries that are neither stored proc or stored function in the current schema don't need to be dropped!
                if (current.State == null) continue;

                var desired = changes.Desired.Get(current);
                if (desired != null)
                {
                    if (changes.SchemaDriver.IsCreateOrReplaceProcSupported)
                    {
                        // If CREATE OR REPLACE PROCEDURE is supported in the current database, there's no need to drop it unless
                        // it's changed from function to procedure or vice versa.
                        if (QueryType.IsTypeEqual(current.State, desired.State)) continue;
                    }
                    else
                    {
                        // Even if CREATE OR REPLACE PROCEDURE is not supported, we don't need to drop it if it's
                        // not changed at all.
                        if (QueryType.IsEqual(current.State, desired.State, changes.DbDriver)) continue;
                    }
                }

                var dropSql = current.State is QueryType.FunctionState
                    ? changes.SchemaDriver.GetDropFunctionSql(current.Name)
                    : changes.SchemaDriver.GetDropProcSql(current.Name);

                if (desired != null)
                {
                    // The query isn't going away entirely so we don't want to lose its associated before statements
                    changes.Put(dropSql, QueryType.CreateUnstored(current.Name));
                }
                else
                {
                    changes.Remove(dropSql, current);
                }
            }
        }
    }
}
