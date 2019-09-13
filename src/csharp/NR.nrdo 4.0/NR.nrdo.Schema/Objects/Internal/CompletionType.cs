using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;

namespace NR.nrdo.Schema.Objects.Internal
{
    public class CompletionType : RootObjectType<CompletionType, Stateless>
    {
        // Global object that represents the fact that an inital run of database creation has completed, so
        // future runs should be considered "upgrades".
        public override string Name { get { return "completion"; } }

        public override IEnumerable<RootObjectState<CompletionType, Stateless>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return GetBasedOnKnownIdentifiers(connection, helper, name => Create());
        }

        public override IEnumerable<StepBase> Steps
        {
            get { yield return new FinishingStep(); }
        }

        public static RootObjectState<CompletionType, Stateless> Create()
        {
            return CreateState("completion", Stateless.Value);
        }
    }
}
