using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Caching;

namespace NR.nrdo.Sql
{
    internal class SqlQueryCache : TableMultiObjectCache<SqlResultRow, SqlQueryWhere, SqlQueryCache>
    {
        private static Dictionary<SqlQueryIdentity, SqlQueryCache> caches = new Dictionary<SqlQueryIdentity, SqlQueryCache>();

        internal static SqlQueryCache Get(SqlQueryBuilder builder)
        {
            var identity = new SqlQueryIdentity(builder);

            lock (Nrdo.LockObj)
            {
                SqlQueryCache result;
                if (caches.TryGetValue(identity, out result)) return result;

                result = new SqlQueryCache(identity, builder);
                caches[identity] = result;

                return result;
            }
        }

        private SqlQueryCache(SqlQueryIdentity identity, SqlQueryBuilder builder)
            : base(builder.DefaultCapacity, builder.DefaultItemCapacity)
        {
            this.identity = identity;
            this.description = builder.Description;

            var setCacheFlushMethod = new SetCacheFlushMethod(this);
            foreach (var table in builder.referencedTables)
            {
                table.InvokeTypedMethod(setCacheFlushMethod);
            }
            this.namedTablesToReactTo = (from table in builder.referencedDynamicTables select SqlDataModification.NamedTable(table)).ToList();
        }

        internal readonly SqlQueryIdentity identity;
        internal readonly string description;
        internal readonly List<SqlDataModification.TableModification> namedTablesToReactTo;

        protected override string GetMethodName()
        {
            return "DYN:" + description;
        }

        public override long ModificationCountHash
        {
            get
            {
                lock (Nrdo.LockObj)
                {
                    return getModificationCountHash() + (from table in namedTablesToReactTo select table.ModificationCount).Sum();
                }
            }
        }

        internal SqlQueryWhere getWhereObject(IEnumerable<SqlValueParam> parameters)
        {
            return new SqlQueryWhere(this, parameters.ToList());
        }

        private Func<long> getModificationCountHash = () => 0;


        private class SetCacheFlushMethod : ITypedMethod
        {
            private readonly SqlQueryCache cache;
            internal SetCacheFlushMethod(SqlQueryCache cache)
            {
                this.cache = cache;
            }

            public void Invoke<TFrom>(TFrom item) where TFrom : DBTableObject<TFrom>, ITableObject
            {
                lock (Nrdo.LockObj)
                {
                    var old = cache.getModificationCountHash;
                    cache.getModificationCountHash = () => old() + DBTableObject<TFrom>.DataModification.Count;
                    DBTableObject<TFrom>.DataModification.Any += cache.Clear;
                }
            }
        }
    }
}
