using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers.Introspection;
using System.Data;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers
{
    public class OracleSchemaDriver : SchemaDriver
    {
        public override DbDriver DbDriver { get { return OracleDriver.Instance; } }

        public string IndexTablespace { get; set; }

        // Field add string
        public override string GetAddFieldSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ADD (" + QuoteIdentifier(fieldName) + " " + GetFieldTypeSpec(datatype, true, isSequenced, sequenceName) + ")";
        }

        // Field drop
        public override string GetDropFieldSql(string tableName, string fieldName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " DROP (" + QuoteIdentifier(fieldName) + ")";
        }

        // Field type alteration
        public override string GetAlterFieldTypeSql(string tableName, string fieldName, string datatype, bool isNullable, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " MODIFY (" + QuoteIdentifier(fieldName) + " " + GetFieldTypeSpec(datatype, isNullable, isSequenced, sequenceName) + ")";
        }

        // Full name for index
        public override string GetIndexName(string tableName, string indexName)
        {
            var schema = ExtractSchema(ref tableName);
            return QuoteIdentifier(schema) + "." + QuoteIdentifier(indexName);
        }

        public override string GetCreateIndexSql(string tableName, string indexName, IEnumerable<string> fieldNames, IndexCustomState customState)
        {
            var result = base.GetCreateIndexSql(tableName, indexName, fieldNames, customState);
            if (IndexTablespace != null) result += " TABLESPACE " + IndexTablespace;
            return result;
        }

        public override string GetCreateUniqueConstraintSql(string tableName, bool isPrimaryKey, string indexName, IEnumerable<string> fieldNames, IndexCustomState customState)
        {
            var result = base.GetCreateUniqueConstraintSql(tableName, isPrimaryKey, indexName, fieldNames, customState);
            if (IndexTablespace != null) result += " USING INDEX TABLESPACE " + IndexTablespace;
            return result;
        }

        // Drop index
        public override string GetDropIndexSql(string tableName, string indexName)
        {
            return "DROP INDEX " + GetIndexName(tableName, indexName);
        }

        public override TriggerTiming GetSequencedFieldTriggerTiming()
        {
            return TriggerTiming.Before;
        }

        public override string GetSequencedFieldTriggerBody(string tableName, string fieldName, string sequenceName)
        {
            return "    IF :new." + QuoteIdentifier(fieldName) + " IS NULL THEN\n"
                + "      SELECT " + QuoteSchemaIdentifier(sequenceName) + ".NEXTVAL\n"
                + "        INTO :new." + QuoteIdentifier(fieldName) + "\n"
                + "        FROM dual;\n    END IF;\n";
        }

        // Add trigger
        public override string GetCreateTriggerSql(string tableName, string triggerName, TriggerTiming timing, TriggerEvents events, string body)
        {
            return "CREATE OR REPLACE TRIGGER " + QuoteSchemaIdentifier(triggerName) + "\n"
                + "  " + timing.ToSql() + " " + events.ToSql() + " ON " + QuoteSchemaIdentifier(tableName) + " FOR EACH ROW\n"
                + "  BEGIN\n" + body + "  END;";
        }

        // Whether dropping and adding sequences and triggers is needed at all
        public override bool IsSequenceUsed { get { return true; } }
        public override bool IsTriggerUsedForSequence { get { return true; } }

        public override string GetDeclareSql(string varname, string vartype)
        {
            return "DECLARE " + varname + " " + vartype + "; BEGIN NULL";
        }

        public override string GetDeclareEndBlockSql(string varname)
        {
            return " END;";
        }

        public override string GetVariableValueSql(string varname)
        {
            return varname;
        }

        public override string GetAssignBeginSql(string varname)
        {
            return varname + " := ";
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
