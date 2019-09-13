using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.OldVersionUpgrade;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.OldVersionUpgrade
{
    internal class OldVersionCacheMigrationType : RootObjectType<OldVersionCacheMigrationType, OldVersionCacheMigrationType.State>
    {
        // Dummy object representing the fact that migration from the old version's nrdo cache has happened OR is unnecessary.
        public override string Name { get { return "old-nrdo-cache-migration"; } }

        public override IEnumerable<RootObjectState<OldVersionCacheMigrationType, OldVersionCacheMigrationType.State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            // This runs before the state table exists, so we wait until the Step is run to figure out what exists.
            yield break;
        }

        public override IEnumerable<StepBase> Steps
        {
            get { yield return new OldVersionCacheMigrationStep(); }
        }

        public struct State
        {
            private readonly Func<IOutput, OldVersionNrdoCache> findOldVersionNrdoCache;
            public Func<IOutput, OldVersionNrdoCache> FindOldVersionNrdoCache { get { return findOldVersionNrdoCache; } }

            internal State(Func<IOutput, OldVersionNrdoCache> findOldVersionNrdoCache)
            {
                this.findOldVersionNrdoCache = findOldVersionNrdoCache;
            }
        }

        public static RootObjectState<OldVersionCacheMigrationType, OldVersionCacheMigrationType.State> Create(Func<IOutput, OldVersionNrdoCache> findOldVersionNrdoCache)
        {
            return CreateState("completed-if-necessary", new State(findOldVersionNrdoCache));
        }
    }
}
