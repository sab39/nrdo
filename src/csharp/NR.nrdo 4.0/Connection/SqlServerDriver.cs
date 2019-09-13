using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace NR.nrdo.Connection
{
    public class SqlServerDriver : DbDriver
    {
        public override string Identifier { get { return "sqlserver"; } }
        public override string DisplayName { get { return "MS SQL Server"; } }

        #region Private constructor and static Instance property

        private static readonly SqlServerDriver instance = new SqlServerDriver();
        public static SqlServerDriver Instance { get { return instance; } }

        private SqlServerDriver() { }

        #endregion

        #region Creating connection objects

        public override IDbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override IDbDataParameter CreateParameter(string name)
        {
            return new SqlParameter { ParameterName = "@" + name };
        }

        #endregion

        #region Identifier quoting

        public override string QuoteIdentifier(string identifier)
        {
            return "[" + identifier + "]";
        }

        #endregion

        #region String comparison

        [Serializable]
        private class SqlServerStringComparer : IEqualityComparer<string>
        {
            internal static readonly SqlServerStringComparer instance = new SqlServerStringComparer();
            private SqlServerStringComparer() { }

            public bool Equals(string x, string y)
            {
                return CollationInfo.Default.EqualityComparer.Equals(x, y);
            }

            public int GetHashCode(string obj)
            {
                return CollationInfo.Default.EqualityComparer.GetHashCode(obj);
            }
        }

        public override IEqualityComparer<string> DbStringComparer { get { return SqlServerStringComparer.instance; } }

        #endregion

        #region SQL Syntax

        // What are the correct substitutions for ::true and ::false?
        public override string TrueSql { get { return "1"; } }
        public override string FalseSql { get { return "0"; } }

        // SQL subsitutions
        public override string ConcatSql { get { return "+"; } }
        public override string NowSql { get { return "{fn Now()}"; } }
        public override string TodaySql { get { return "{fn Today()}"; } }

        public override string GetNewSequencedKeyValueSql(string sequenceName)
        {
            return "CAST(@@identity AS int)";
            // FIXME - scope_identity()?
            // FIXME - what about identities that are of long type or whatever?
        }

        #endregion

    }
}
