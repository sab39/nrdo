using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers.Introspection;

namespace NR.nrdo.Schema.Drivers
{
    public class PostgresSchemaDriver : SchemaDriver
    {
        public override DbDriver DbDriver { get { return PostgresDriver.Instance; } }

        // Field set not null
        public override string GetSetFieldNotNullSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ALTER COLUMN " + QuoteIdentifier(fieldName) + " SET NOT NULL";
        }

        public override string GetSetFieldNullSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ALTER COLUMN " + QuoteIdentifier(fieldName) + " DROP NOT NULL";
        }

        // How identity fields differ from non-identity fields
        public override string GetSequencedFieldTypeSpec(string datatype, bool isNullable, string sequenceName)
        {
            return datatype + " DEFAULT NEXTVAL('" + sequenceName + "')" + (isNullable ? "" : " NOT") + " NULL";
        }

        // Drop index
        public override string GetDropIndexSql(string tableName, string indexName)
        {
            return "DROP INDEX " + QuoteIdentifier(indexName);
        }

        // Whether CREATE OR REPLACE PROCEDURE is supported (used twice)
        public override bool IsCreateOrReplaceProcSupported { get { return true; } }

        public override bool IsSequenceUsed { get { return true; } }

        public override string GetDeclareSql(string varname, string vartype)
        {
            return "create temp table nrdovar_" + varname + " (" + varname + " "
                + vartype + " null); insert into nrdovar_" + varname + " (" + varname
                + ") values (null)";
        }

        public override string GetVariableValueSql(string varname)
        {
            return "(select " + varname + " from nrdovar_" + varname + ")";
        }

        public override string GetAssignBeginSql(string varname)
        {
            return "update nrdovar_" + varname + " set " + varname + " = ";
        }

        public override string GetDeclareEndBlockSql(string varname)
        {
            return "drop table nrdovar_" + varname + ";";
        }

        public override IEnumerable<IntrospectedIndex> GetAllNonUniqueIndexes(NrdoConnection connection)
        {
            throw new NotImplementedException("Determining existing non-unique indexes is not supported on " + DisplayName);
        }

        public override IEnumerable<IntrospectedTrigger> GetAllTriggers(NrdoConnection connection)
        {
            throw new NotImplementedException("Determining existing triggers is not supported on " + DisplayName);
        }
    }
}
