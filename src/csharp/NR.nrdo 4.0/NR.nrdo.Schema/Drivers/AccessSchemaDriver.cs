using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers.Introspection;

namespace NR.nrdo.Schema.Drivers
{
    public class AccessSchemaDriver : SchemaDriver
    {
        public override DbDriver DbDriver { get { return AccessDriver.Instance; } }

        // Field add string
        public override string GetAddFieldSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ADD COLUMN " + QuoteIdentifier(fieldName) + " " + GetFieldTypeSpec(datatype, true, isSequenced, sequenceName);
        }

        // How identity fields differ from non-identity fields
        public override string GetSequencedFieldTypeSpec(string datatype, bool isNullable, string sequenceName)
        {
            if (isNullable) throw new ArgumentException("Access does not support nullable AUTOINCREMENT fields");
            return "AUTOINCREMENT";
        }

        // Whether an identity field can be created as nullable and altered, or
        // whether it must be created non-null off the bat.
        public override bool IsAlterSequencedFieldSupported { get { return false; } }

        // Drop index
        public override string GetDropIndexSql(string tableName, string indexName)
        {
            return "DROP INDEX " + QuoteIdentifier(indexName) + " ON " + QuoteSchemaIdentifier(tableName);
        }

        public override string QuoteParam(string name)
        {
            return "@" + name;
        }

        public override string UnquoteParam(string name)
        {
            return name.TrimStart('@');
        }

        public override string ParamNameRegex { get { return "@" + base.ParamNameRegex; } }

        public override string IdentifierRegex { get { return @"( \w+ | (\[ [^][]+ \]))"; } }

        // Whether CREATE OR REPLACE PROCEDURE is supported (used twice)
        public override bool IsCreateOrReplaceProcSupported { get { return false; } }

        // Whether to execute a 200ms sleep after executing an SQL statement
        public override bool IsSleepRequiredBetweenStatements { get { return false; } }

        public override string GetDeclareSql(String varname, String vartype)
        {
            throw new NotImplementedException("::declare not yet implemented for access");
        }

        public override string GetVariableValueSql(String varname)
        {
            throw new NotImplementedException("::var not yet implemented for access");
        }

        public override string GetAssignBeginSql(String varname)
        {
            throw new NotImplementedException("::assign not yet implemented for access");
        }

        public override string CommandSeparator
        {
            get
            {
                return "\r\nGO\r\n";
            }
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
