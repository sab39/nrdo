using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Queries
{
    public sealed class PreUpgradeHookType : SubObjectType<PreUpgradeHookType, Stateless, QueryType, QueryType.ProcState>
    {
        // There's no actual data, not even an identifier, associated with the pre-upgrade-hook object state. Its existence
        // indicates that its parent stored-proc QueryState is a pre-upgrade-hook and should be run as such.

        public override string Name { get { return "pre-upgrade-hook"; } }

        public override IEnumerable<SubObjectState<PreUpgradeHookType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetBasedOnKnownIdentifiers(connection, helper, (query, name) => Create(query));
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new PreUpgradeHooksStep();
                yield return new DropStoredProcsStep();
                yield return new AddStoredProcsStep();
            }
        }

        public static SubObjectState<PreUpgradeHookType, Stateless> Create(Identifier query)
        {
            return CreateState(query, "pre-upgrade-hook", Stateless.Value);
        }
        public static SubObjectState<PreUpgradeHookType, Stateless> Create(string queryName)
        {
            return Create(QueryType.Identifier(queryName));
        }
    }
}
