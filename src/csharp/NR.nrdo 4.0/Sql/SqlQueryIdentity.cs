using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Sql
{
    internal class SqlQueryIdentity
    {
        internal SqlQueryIdentity(SqlQueryBuilder builder)
        {
            this.dataBase = builder.DataBase;
            this.sqlStatement = builder.SqlStatement;
            this.columnNames = builder.ResultColumnNames.ToList();
            this.paramNames = builder.ParameterNames.ToList();
        }

        internal readonly DataBase dataBase;
        internal readonly string sqlStatement;
        internal readonly List<string> columnNames;
        internal readonly List<string> paramNames;

        public override bool Equals(object obj)
        {
            var other = obj as SqlQueryIdentity;
            if (other == null) return false;

            return dataBase.Name == other.dataBase.Name &&
                sqlStatement == other.sqlStatement &&
                columnNames.SequenceEqual(other.columnNames, StringComparer.OrdinalIgnoreCase) &&
                paramNames.SequenceEqual(other.paramNames, StringComparer.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return (dataBase.Name == null ? 0 : dataBase.Name.GetHashCode()) +
                sqlStatement.GetHashCode() +
                columnNames.Sum(name => StringComparer.OrdinalIgnoreCase.GetHashCode(name)) +
                paramNames.Sum(name => StringComparer.OrdinalIgnoreCase.GetHashCode(name));
        }
    }
}
