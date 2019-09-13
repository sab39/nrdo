using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OleDb;

namespace NR.nrdo.Connection
{
    public class AccessDriver : DbDriver
    {
        public override string Identifier { get { return "access"; } }
        public override string DisplayName { get { return "MS Access"; } }

        #region Private constructor and static Instance property

        private static readonly AccessDriver instance = new AccessDriver();
        public static AccessDriver Instance { get { return instance; } }

        private AccessDriver() { }

        #endregion

        #region Creating connection objects

        public override IDbConnection CreateConnection(string connectionString)
        {
            return new OleDbConnection(connectionString);
        }

        protected override IDbDataParameter CreateParameter(string name)
        {
            return new OleDbParameter { ParameterName = name };
        }

        #endregion

        #region Identifier quoting

        public override string QuoteIdentifier(string identifier)
        {
            return "[" + identifier + "]";
        }

        #endregion

        #region SQL Syntax

        // What are the correct substitutions for ::true and ::false?
        public override string TrueSql { get { return "-1"; } }
        public override string FalseSql { get { return "0"; } }

        public override string NowSql { get { return "Now()"; } }
        public override string TodaySql { get { return "Today()"; } }

        public override string ConcatSql { get { return "+"; } }

        public override string GetNewSequencedKeyValueSql(string sequenceName)
        {
            return "@@identity";
        }

        #endregion

    }
}
