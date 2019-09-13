using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Providers
{
    public sealed class EssentialSchemaProvider : ISchemaProvider
    {
        public IEnumerable<ObjectType> GetObjectTypes(SchemaDriver schemaDriver)
        {
            yield return PendingReorderTableType.Instance;
            yield return BeforeStatementType.Instance;
            yield return CompletionType.Instance;
        }

        public IEnumerable<ObjectState> GetDesiredState(SchemaConnection connection, IOutput output)
        {
            yield return CompletionType.Create();
        }
    }
}
