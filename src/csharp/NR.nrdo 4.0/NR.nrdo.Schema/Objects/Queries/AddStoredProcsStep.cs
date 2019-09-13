using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Views;

namespace NR.nrdo.Schema.Objects.Queries
{
    public sealed class AddStoredProcsStep : StepBase
    {
        public override string Identifier { get { return "adding-storedprocs"; } }

        public override bool MustHappenAfter(StepBase other)
        {
            return other is DropStoredProcsStep || other is AddViewsStep || other is AddFkeysStep;
        }

        public override bool MustHappenBefore(StepBase other)
        {
            return other is DropFieldsStep;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            foreach (var desired in QueryType.AllFrom(changes.Desired))
            {
                var current = changes.Current.Get(desired);
                if (current != null && QueryType.IsEqual(current.State, desired.State, changes.DbDriver)) continue;

                if (desired.State != null)
                {
                    var body = desired.State.Body;
                    var parameters = desired.State.Parameters;
                    string createSql;

                    var functionState = desired.State as QueryType.FunctionState;
                    if (functionState != null)
                    {
                        createSql = changes.SchemaDriver.GetCreateFunctionSql(desired.Name, parameters, functionState.ReturnType, body);
                    }
                    else
                    {
                        createSql = changes.SchemaDriver.GetCreateProcSql(desired.Name, parameters, body);
                    }

                    changes.Put(createSql, desired);
                }
                else
                {
                    if (current == null) changes.Put(null, desired);
                }
            }

            // Restore all pre-upgrade-hooks corresponding to procedures that have been successfully updated.
            foreach (var upgradeHook in PreUpgradeHookType.AllFrom(changes.Desired))
            {
                var desiredProc = QueryType.GetFrom(changes.Desired, upgradeHook.ParentIdentifier);

                var currentProc = changes.Current.Get(desiredProc);
                if (currentProc != null && QueryType.IsEqual(currentProc.State, desiredProc.State, changes.DbDriver))
                {
                    changes.Put(null, upgradeHook);
                }
            }
        }
    }
}
