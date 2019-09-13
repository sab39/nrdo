using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public class OracleDriver : DbDriver
    {
        public override string Identifier { get { return "oracle"; } }
        public override string DisplayName { get { return "Oracle"; } }

        #region Private constructor and static Instance property

        private static readonly OracleDriver instance = new OracleDriver();
        public static OracleDriver Instance { get { return instance; } }

        private OracleDriver() { }

        #endregion

        #region Creating connection objects - FIXME: Not implemented, don't actually know what classes to use

        public override IDbConnection CreateConnection(string connectionString)
        {
            throw new NotImplementedException(); // FIXME
        }

        protected override IDbDataParameter CreateParameter(string name)
        {
            throw new NotImplementedException(); // FIXME
        }

        #endregion

        #region Identifier quoting

        // What quote characters are used?
        // Quoting things in Oracle makes them case-sensitive where they wouldn't normally be, which isn't what we want.
        // So we don't quote things at all unless they are SQL keywords in which case we lowercase and quote them.
        // FIXME currently we don't actually have the list of SQL keywords so nothing gets quoted
        // FIXME research whether there's a way to use quoted case-insensitive identifiers to avoid this hack
        public override string QuoteIdentifier(string identifier)
        {
            return IsSqlKeyword(identifier) ? base.QuoteIdentifier(identifier.ToLowerInvariant()) : identifier;
        }

        public bool IsSqlKeyword(string identifier)
        {
            // FIXME implement a list of oracle sql keywords that require quoting
            return false;
        }

        #endregion

        #region SQL Syntax

        // What are the correct substitutions for ::true and ::false?
        public override string TrueSql { get { return "'Y'"; } }
        public override string FalseSql { get { return "'N'"; } }

        // SQL subsitutions
        public override string NowSql { get { return "SYSDATE"; } }
        public override string TodaySql { get { return "TRUNC(SYSDATE, 'J')"; } }

        public override string GetSelectFromNothingSql(string selectClause)
        {
            return "SELECT " + selectClause + " FROM DUAL";
        }
        
        public override string GetNewSequencedKeyValueSql(string sequenceName)
        {
            return QuoteSchemaIdentifier(sequenceName) + ".CURRVAL";
        }

        #endregion

        #region Data types

        public override IDataParameter CreateBoolParameter(string name, string dataType, bool? value)
        {
            char? ch = null;
            if (value != null) ch = value.Value ? 'Y' : 'N';
            return CreateCharParameter(name, dataType, ch);
        }

        public override bool ReadBoolValue(IDataReader dr, int index)
        {
            return base.ReadBoolValue(dr, index);
        }

        #endregion
    }
}
