using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Shared
{
    [Flags]
    public enum TriggerEvents
    {
        Insert = 1,
        Update = 2,
        Delete = 4
    }
}
