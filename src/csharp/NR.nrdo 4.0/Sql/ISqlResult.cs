using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Sql
{
    public interface ISqlResult
    {
        object this[string columnName] { get; }
    }
}
