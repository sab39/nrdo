using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using NR.nrdo.Connection;

namespace NR.nrdo.Schema.Drivers
{
    public sealed class SqlServerIndexCustomState : IndexCustomState
    {
        private readonly bool isClustered;
        public bool IsClustered { get { return isClustered; } }

        private readonly ImmutableHashSet<string> includedFields;
        public ImmutableHashSet<string> IncludedFields { get { return includedFields; } }

        public SqlServerIndexCustomState(bool isClustered, IEnumerable<string> includedFields)
        {
            this.isClustered = isClustered;
            this.includedFields = (includedFields ?? Enumerable.Empty<string>()).ToImmutableHashSet(SqlServerDriver.Instance.DbStringComparer);
        }

        private static readonly SqlServerIndexCustomState clustered = new SqlServerIndexCustomState(true, null);
        public static SqlServerIndexCustomState Clustered { get { return clustered; } }

        private static readonly SqlServerIndexCustomState nonClustered = new SqlServerIndexCustomState(false, null);
        public static SqlServerIndexCustomState NonClustered { get { return nonClustered; } }

        public string ClusteringKeyword { get { return isClustered ? "CLUSTERED" : "NONCLUSTERED"; } }

        public override string CustomStateType
        {
            get { return "Explicit " + ClusteringKeyword + " specification" + (IncludedFields.Any() ? " and INCLUDEd fields" : ""); }
        }

        public override string ToString()
        {
            return ClusteringKeyword + (IncludedFields.Any() ? " INCLUDE (" + string.Join(", ", IncludedFields) + ")" : "");
        }
    }
}
