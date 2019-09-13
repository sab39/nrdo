using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Reflection
{
    public interface INrdoCodeBase
    {
        IEnumerable<NrdoTable> AllTables { get; }
        NrdoTable GetTable(string tableName);

        IDictionary<string, string> GetTableRenameMapping();

        IEnumerable<NrdoQuery> AllQueries { get; }
        NrdoQuery GetQuery(string queryName);
    }
}
