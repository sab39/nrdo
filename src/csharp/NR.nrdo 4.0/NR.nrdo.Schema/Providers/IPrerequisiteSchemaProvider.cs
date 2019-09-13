using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Providers
{
    internal interface IPrerequisiteSchemaProvider : ISchemaProvider
    {
        bool IncludeInNormalRun { get; }
    }
}
