using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Providers
{
    public interface ISchemaOverrideProvider : ISchemaProvider
    {
        DatabaseState ApplyOverrides(SchemaConnection connection, DatabaseState desiredState);
    }
}
