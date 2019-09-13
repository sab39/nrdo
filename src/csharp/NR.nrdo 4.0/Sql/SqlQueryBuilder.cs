using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Reflection;

namespace NR.nrdo.Sql
{
    public class SqlQueryBuilder
    {
        public static SqlQueryBuilder Create(DataBase dataBase)
        {
            return new SqlQueryBuilder(dataBase);
        }

        public SqlQuery<T> GetQuery<T>(Func<ISqlResult, T> createResult)
        {
            if (SqlStatement == null) throw new ArgumentNullException("SqlStatement");
            if (!ResultColumnNames.Any()) throw new ArgumentException("Must specify result columns for SqlQuery");

            return new SqlQuery<T>(createResult, SqlQueryCache.Get(this));
        }

        public SqlQueryCall<T> GetQueryCall<T>(Func<ISqlResult, T> createResult)
        {
            if (ParameterNames.Any())
            {
                throw new ArgumentException("GetQueryCall is only for use on queries with no parameters");
            }
            return GetQuery(createResult).GetCall();
        }

        private SqlQueryBuilder(DataBase dataBase)
        {
            this.DataBase = dataBase;
            this.ResultColumnNames = new List<string>();
            this.ParameterNames = new List<string>();

            if (dataBase.Equals(DataBase.Default))
            {
                DefaultCapacity = 20;
                DefaultItemCapacity = 200;
            }
            else
            {
                DefaultCapacity = 0;
                DefaultItemCapacity = 0;
            }
        }

        public DataBase DataBase { get; private set; }

        private string description;
        public string Description
        {
            get { return description ?? "Dynamic query " + SqlStatement.GetHashCode().ToString("X"); }
            set { description = value; }
        }

        public string SqlStatement { get; set; }

        public List<string> ResultColumnNames { get; private set; }

        public List<string> ParameterNames { get; private set; }

        public int DefaultCapacity { get; set; }
        public int DefaultItemCapacity { get; set; }

        internal readonly HashSet<NrdoTableIdentity> referencedTables = new HashSet<NrdoTableIdentity>();
        internal readonly HashSet<string> referencedDynamicTables = new HashSet<string>();

        public void AddReferencedTable<T>()
            where T : DBTableObject<T>
        {
            AddReferencedTable(NrdoTableIdentity.Get<T>());
        }

        public void AddReferencedTable(NrdoTableIdentity table)
        {
            if (!referencedTables.Contains(table)) referencedTables.Add(table);
        }

        public void AddNamedTable(string table)
        {
            if (!referencedDynamicTables.Contains(table)) referencedDynamicTables.Add(table);
        }
    }
}
