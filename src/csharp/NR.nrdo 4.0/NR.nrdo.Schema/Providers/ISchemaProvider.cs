using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Providers
{
    public interface ISchemaProvider
    {
        IEnumerable<ObjectType> GetObjectTypes(SchemaDriver schemaDriver);
        IEnumerable<ObjectState> GetDesiredState(SchemaConnection connection, IOutput output);
    }
}
