using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Providers;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.OldVersionUpgrade
{
    public sealed class OldVersionUpgradeProvider : ISchemaProvider, IPrerequisiteSchemaProvider
    {
        private readonly Func<IOutput, OldVersionNrdoCache> findOldVersionNrdoCache;
        public OldVersionUpgradeProvider(Func<IOutput, OldVersionNrdoCache> findOldVersionNrdoCache)
        {
            if (findOldVersionNrdoCache == null) throw new ArgumentNullException("findOldVersionNrdoCache");
            this.findOldVersionNrdoCache = findOldVersionNrdoCache;
        }

        public bool IncludeInNormalRun { get { return false; } }

        public IEnumerable<ObjectType> GetObjectTypes(SchemaDriver schemaDriver)
        {
            yield return new OldVersionCacheMigrationType();
        }

        public IEnumerable<ObjectState> GetDesiredState(SchemaConnection connection, IOutput output)
        {
            yield return OldVersionCacheMigrationType.Create(findOldVersionNrdoCache);
        }
    }
}
