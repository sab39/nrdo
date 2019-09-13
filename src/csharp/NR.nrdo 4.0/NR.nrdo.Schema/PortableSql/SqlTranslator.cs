using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.PortableSql
{
    public class SqlTranslator : SqlTranslatorBase
    {
        private readonly SchemaDriver dbDriver;
        private readonly Func<string, string> getSequenceName;

        private StringBuilder sb;

        public SqlTranslator(SchemaDriver dbDriver, Func<string, string> getSequenceName)
        {
            this.dbDriver = dbDriver;
            this.getSequenceName = getSequenceName;
        }

        public string Translate(string input)
        {
            lock (this)
            {
                // Assume that translation will cause the string to grow by at least 20 characters or 10%
                sb = new StringBuilder(input.Length + Math.Max(20, input.Length / 10));
                Process(input);
                var result = sb.ToString();
                sb = null;
                return result;
            }
        }

        protected override void ReadParameter(string paramName)
        {
            sb.Append(dbDriver.QuoteParam(paramName));
        }
        protected override void ReadIdentifier(string identifier)
        {
            sb.Append(dbDriver.DbDriver.QuoteSchemaIdentifier(identifier));
        }

        protected override void ReadSqlChar(char ch)
        {
            sb.Append(ch);
        }

        protected override void ReadSqlString(string str)
        {
            sb.Append(str);
        }

        protected override void ReadSyntaxError(string errorMessage)
        {
            throw new ApplicationException(errorMessage);
        }

        protected override void ReadSqlForSequenceValue(string tableName)
        {
            sb.Append(dbDriver.DbDriver.GetNewSequencedKeyValueSql(getSequenceName(tableName)));
        }

        protected override void ReadSqlForVariableValue(string varName)
        {
            sb.Append(dbDriver.GetVariableValueSql(varName));
        }

        protected override void ReadSqlForDeclare(string varName, string varType, out Action finishAction)
        {
            sb.Append(dbDriver.GetDeclareSql(varName, varType));
            finishAction = () => sb.Append(dbDriver.GetDeclareEndBlockSql(varName));
        }

        protected override void ReadSqlForAssign(string varName, out Action closeAction)
        {
            sb.Append(dbDriver.GetAssignBeginSql(varName));
            closeAction = () => sb.Append(dbDriver.GetAssignEndSql(varName));
        }

        protected override void ReadSqlForNow()
        {
            sb.Append(dbDriver.DbDriver.NowSql);
        }

        protected override void ReadSqlForToday()
        {
            sb.Append(dbDriver.DbDriver.TodaySql);
        }

        protected override void ReadSqlForTrue()
        {
            sb.Append(dbDriver.DbDriver.TrueSql);
        }

        protected override void ReadSqlForFalse()
        {
            sb.Append(dbDriver.DbDriver.FalseSql);
        }

        protected override void ReadSqlForConcat()
        {
            sb.Append(dbDriver.DbDriver.ConcatSql);
        }
    }
}
