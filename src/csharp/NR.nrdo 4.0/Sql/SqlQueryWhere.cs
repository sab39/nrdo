using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Caching;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Sql
{
    internal class SqlQueryWhere : CachingWhereBase<SqlResultRow, SqlQueryWhere, SqlQueryCache>
    {
        internal SqlQueryWhere(SqlQueryCache cache, List<SqlValueParam> parameters)
        {
            this.cache = cache;
            this.parameters = parameters;

            if (cache.identity.paramNames.Count != parameters.Count) throw new ArgumentException("Expected " + cache.identity.paramNames.Count + " parameters, got " + parameters.Count);
        }

        internal readonly SqlQueryCache cache;
        private readonly List<SqlValueParam> parameters;

        public override IDBObjectCache<SqlResultRow> Cache
        {
            get { return cache; }
        }

        public override void SetOnCmd(NrdoCommand cmd)
        {
            for (var i = 0; i < cache.identity.paramNames.Count; i++)
            {
                parameters[i].SetParameter(cmd, cache.identity.paramNames[i]);
            }
        }

        public override string SQLStatement
        {
            get { return cache.identity.sqlStatement; }
        }

        public override string GetMethodName
        {
            get { return "DYN:" + cache.description; }
        }

        public override bool Equals(object obj)
        {
            var other = obj as SqlQueryWhere;
            if (other == null) return false;

            if (other.cache != this.cache) return false;

            return Enumerable.SequenceEqual(
                from param in parameters select param.GetObjectValue(),
                from param in other.parameters select param.GetObjectValue());

        }

        public override int GetHashCode()
        {
            var hc = 0;
            for (var i = 0; i < parameters.Count; i++)
            {
                var obj = parameters[i].GetObjectValue();
                if (obj != null) hc += i * obj.GetHashCode();
            }
            return hc;
        }

        public override string GetParameters
        {
            get { return string.Join(", ", parameters); }
        }
    }
}
