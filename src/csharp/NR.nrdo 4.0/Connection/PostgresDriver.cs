using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public class PostgresDriver : DbDriver
    {
        public override string Identifier { get { return "postrgres"; } }
        public override string DisplayName { get { return "PostgreSQL"; } }

        #region Private constructor and static Instance property

        private static readonly PostgresDriver instance = new PostgresDriver();
        public static PostgresDriver Instance { get { return instance; } }

        private PostgresDriver() { }

        #endregion

        #region Creating connection objects

        public override IDbConnection CreateConnection(string connectionString)
        {
#if POSTGRES
            return new NpgsqlConnection(connectionString);
#else
            throw new NotImplementedException("Postgres support not compiled");
#endif
        }

        protected override IDbDataParameter CreateParameter(string name)
        {
#if POSTGRES
            return new NpgsqlParameter { ParameterName = name };
#else
            throw new NotImplementedException("Postgres support not compiled");
#endif
        }

        #endregion

        #region SQL Syntax

        public override string NowSql { get { return "current_timestamp"; } }
        public override string TodaySql { get { return "current_date"; } }

        public override string GetNewSequencedKeyValueSql(string sequenceName)
        {
            ExtractSchema(ref sequenceName);
            return "currval('" + sequenceName + "')";
        }

        #endregion

    }
}
