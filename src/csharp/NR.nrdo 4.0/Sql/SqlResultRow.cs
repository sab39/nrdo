using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Sql
{
    internal class SqlResultRow : DBTableObject<SqlResultRow>, ISqlResult
    {
        private readonly List<string> columnNames;
        private readonly List<object> values;

        private SqlResultRow(List<string> columnNames, List<object> values)
        {
            this.columnNames = columnNames;
            this.values = values;
        }
        private SqlResultRow(List<string> columnNames, IDataReader reader)
        {
            this.columnNames = columnNames;
            this.values = (from name in columnNames select reader[name]).ToList();
        }

        private int getOrdinal(string columnName)
        {
            return columnNames.FindIndex(name => name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
        }

        public object this[string columnName]
        {
            get { return values[getOrdinal(columnName)]; }
        }

        public class DynamicWhere : Where<SqlResultRow>
        {
            public DynamicWhere(string description, string sqlStatement, Action<NrdoCommand> setParameterValues = null)
            {
                this.description = description;
                this.sqlStatement = sqlStatement;
                this.setParameterValues = setParameterValues;
            }

            private string description;
            private string sqlStatement;
            private Action<NrdoCommand> setParameterValues;

            public override string GetMethodName
            {
                get { return "Dynamic Get: " + description; }
            }

            public override string SQLStatement
            {
                get { return sqlStatement; }
            }

            public override void SetOnCmd(NrdoCommand cmd)
            {
                if (setParameterValues != null) setParameterValues(cmd);
            }
        }

        public static IEnumerable<SqlResultRow> get(SqlQueryWhere whereObject)
        {
            lock (Nrdo.LockObj)
            {
                if (!nrdoInitialized) return null; // can never happen but we get unused warnings on the variable otherwise
                nrdoInitialize(() => whereObject.cache.identity.dataBase,
                    result => new SqlResultRow(whereObject.cache.identity.columnNames, result.Reader),
                    "");
                return getMulti(whereObject.cache.identity.dataBase, whereObject);
            }
        }

        protected internal override SqlResultRow FieldwiseClone()
        {
            return new SqlResultRow(columnNames, values);
        }

        #region Code that looks like a real generated table object
        private static bool nrdoInitialized = true; // Some things try to read this by reflection

        public override int GetPkeyHashCode()
        {
            throw new NotImplementedException();
        }
        public override bool PkeyEquals(SqlResultRow t)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Methods that don't need to be implemented since modification goes through a different codepath
        protected override string DeleteStatement
        {
            get { throw new NotImplementedException(); }
        }

        protected override string InsertStatement
        {
            get { throw new NotImplementedException(); }
        }

        protected override string UpdateStatement
        {
            get { throw new NotImplementedException(); }
        }

        protected override void getPkeyFromSeq(NrdoScope scope)
        {
            throw new NotImplementedException();
        }

        protected override void setDataOnCmd(NrdoCommand cmd)
        {
            throw new NotImplementedException();
        }

        protected override void setPkeyOnCmd(NrdoCommand cmd)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
