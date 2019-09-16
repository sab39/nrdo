using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers.Introspection;
using System.Data;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Connection;
using System.Text.RegularExpressions;

namespace NR.nrdo.Schema.Drivers
{
    public abstract class SchemaDriver
    {
        #region Driver for basic DB access

        public abstract DbDriver DbDriver { get; }

        public string DisplayName { get { return DbDriver.DisplayName; } }

        #endregion

        #region Tables and Fields

        // Create table (not currently different based on DBs)
        public virtual string GetCreateTableSql(string tableName, IEnumerable<FieldCreation> fields)
        {
            return "CREATE TABLE " + QuoteSchemaIdentifier(tableName) + " (" +
                string.Join(",", from field in fields
                                 select "\r\n  " + QuoteIdentifier(field.FieldName) + " " +
                                    GetFieldTypeSpec(field.Datatype, field.IsNullable, field.IsSequencedPkey, field.SequenceName)) +
                ")";
        }

        // Drop table (not currently different based on DBs)
        public virtual string GetDropTableSql(string tableName)
        {
            return "DROP TABLE " + QuoteSchemaIdentifier(tableName);
        }

        // Rename table
        public virtual string GetRenameTableSql(string oldTableName, string newTableName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(oldTableName) + " RENAME TO " + QuoteSchemaIdentifier(newTableName);
        }

        // Create table from Select statement
        public virtual string GetCreateTableAsSelectSql(string tableName, string selectClause, string fromClause)
        {
            return "CREATE TABLE " + QuoteSchemaIdentifier(tableName) + " AS SELECT " + selectClause + " FROM " + fromClause;
        }

        // Add a field to an existing table
        public virtual string GetAddFieldSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            var createNullable = true;
            if (isSequenced && !IsAlterSequencedFieldSupported) createNullable = false;
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ADD " + QuoteIdentifier(fieldName) + " " + GetFieldTypeSpec(datatype, createNullable, isSequenced, sequenceName);
        }

        // Drop a field from an existing table
        public virtual string GetDropFieldSql(string tableName, string fieldName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " DROP COLUMN " + QuoteIdentifier(fieldName);
        }

        // Make a field NOT NULL
        public virtual string GetSetFieldNotNullSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return GetAlterFieldTypeSql(tableName, fieldName, datatype, false, isSequenced, sequenceName);
        }

        // Make a field nullable
        public virtual string GetSetFieldNullSql(string tableName, string fieldName, string datatype, bool isSequenced, string sequenceName)
        {
            return GetAlterFieldTypeSql(tableName, fieldName, datatype, true, isSequenced, sequenceName);
        }

        // Change the type of a field
        public virtual string GetAlterFieldTypeSql(string tableName, string fieldName, string datatype, bool isNullable, bool isSequenced, string sequenceName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ALTER COLUMN "
                + QuoteIdentifier(fieldName) + " " + GetFieldTypeSpec(datatype, isNullable, isSequenced, sequenceName);
        }

        // How an individual field is created (not currently different based on DBs)
        public virtual string GetTypeSpec(string datatype, bool isNullable)
        {
            return datatype + (isNullable ? "" : " NOT") + " NULL";
        }

        // How a field is created (not currently different based on DBs)
        public virtual string GetFieldTypeSpec(string datatype, bool isNullable, bool isSequenced, string sequenceName)
        {
            return isSequenced ? GetSequencedFieldTypeSpec(datatype, isNullable, sequenceName) : GetTypeSpec(datatype, isNullable);
        }
        #endregion

        #region Sequenced primary key support (identity fields / sequences)

        // Whether sequenced primary keys use separate sequence objects, or triggers
        public virtual bool IsSequenceUsed { get { return false; } }
        public virtual bool IsTriggerUsedForSequence { get { return false; } }

        // Whether sequenced fields can be ALTERed or must be dropped/added
        public virtual bool IsAlterSequencedFieldSupported { get { return true; } }

        // Whether making a field sequenced or not is part of the field declaration and therefore requires
        // an ALTER FIELD, or is entirely separate.
        // So far the only way it can not be part of the field declaration is if it's set by a trigger.
        public virtual bool IsSequencedPartOfFieldDeclaration { get { return !IsTriggerUsedForSequence; } }

        // Override this with the syntax required for identity fields
        public virtual string GetSequencedFieldTypeSpec(string datatype, bool isNullable, string sequenceName)
        {
            if (isNullable && !IsAlterSequencedFieldSupported)
            {
                throw new ArgumentException("Cannot create nullable sequenced key field because it can't be altered to notnull later.");
            }
            return GetTypeSpec(datatype, isNullable);
        }

        public virtual TriggerTiming GetSequencedFieldTriggerTiming()
        {
            if (!IsTriggerUsedForSequence) throw new NotSupportedException("This database does not use triggers for sequenced keys");
            throw new NotImplementedException("This database driver uses triggers for sequenced keys but did not override GetSequencedFieldTriggerTiming. That's a bug.");
        }

        // Trigger body used for sequenced keys
        public virtual string GetSequencedFieldTriggerBody(string tableName, string fieldName, string sequenceName)
        {
            if (!IsTriggerUsedForSequence) throw new NotSupportedException("This database does not use triggers for sequenced keys");
            throw new NotImplementedException("This database driver uses triggers for sequenced keys but did not override GetSequencedFieldTriggerBody. That's a bug.");
        }

        // Add sequence (not currently different based on DBs)
        public virtual string GetCreateSequenceSql(string sequenceName)
        {
            return "CREATE SEQUENCE " + QuoteSchemaIdentifier(sequenceName);
        }

        // Drop sequence (not currently different based on DBs)
        public virtual string GetDropSequenceSql(string sequenceName)
        {
            return "DROP SEQUENCE " + QuoteSchemaIdentifier(sequenceName);
        }

        #endregion

        #region Indexes

        public virtual string GetUniqueConstraintKeyword(bool isPrimaryKey, IndexCustomState customState)
        {
            return customState == null ? GetUniqueConstraintKeyword(isPrimaryKey) : GetCustomizedUniqueConstraintKeyword(isPrimaryKey, customState);
        }
        public virtual string GetUniqueConstraintKeyword(bool isPrimaryKey)
        {
            return isPrimaryKey ? "PRIMARY KEY" : "UNIQUE";
        }
        public virtual string GetCustomizedUniqueConstraintKeyword(bool isPrimaryKey, IndexCustomState customState)
        {
            throw new NotImplementedException(customState.CustomStateType + " is not a supported feature on " + DisplayName);
        }

        public virtual IndexCustomState DefaultUniqueConstraintCustomState { get { return null; } }
        public virtual IndexCustomState DefaultPrimaryKeyCustomState { get { return null; } }

        // Create unique (pkey/unique) index
        public virtual string GetCreateUniqueConstraintSql(string tableName, bool isPrimaryKey, string indexName, IEnumerable<string> fieldNames, IndexCustomState customState)
        {

            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " ADD CONSTRAINT " + QuoteIdentifier(indexName) + " " +
              GetUniqueConstraintKeyword(isPrimaryKey, customState) + " (" + string.Join(", ", from field in fieldNames select QuoteIdentifier(field)) + ")";
        }

        // Drop unique (pkey/unique) index (not currently different based on DBs)
        public virtual string GetDropUniqueConstraintSql(string tableName, string indexName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " DROP CONSTRAINT " + QuoteIdentifier(indexName);
        }

        // Full name for an index
        public virtual string GetIndexName(string tableName, string indexName)
        {
            return QuoteIdentifier(indexName);
        }

        public virtual string GetIndexKeyword(IndexCustomState customState)
        {
            return customState == null ? "INDEX" : GetCustomizedIndexKeyword(customState);
        }

        public virtual string GetCustomizedIndexKeyword(IndexCustomState customState)
        {
            throw new NotImplementedException(customState.CustomStateType + " is not a supported feature on " + DisplayName);
        }

        public virtual string GetCreateIndexSuffix(IndexCustomState customState)
        {
            return customState == null ? "" : GetCustomizedCreateIndexSuffix(customState);
        }

        public virtual string GetCustomizedCreateIndexSuffix(IndexCustomState customState)
        {
            throw new NotImplementedException(customState.CustomStateType + " is not a supported feature on " + DisplayName);
        }

        public virtual IndexCustomState DefaultIndexCustomState { get { return null; } }

        // Create index
        public virtual string GetCreateIndexSql(string tableName, string indexName, IEnumerable<string> fieldNames, IndexCustomState customState)
        {
            return "CREATE " + GetIndexKeyword(customState) + " " + GetIndexName(tableName, indexName) + " ON " + QuoteSchemaIdentifier(tableName) + " (" +
                string.Join(", ", from field in fieldNames select QuoteIdentifier(field)) +
                ")" + GetCreateIndexSuffix(customState);
        }

        // Drop index
        public abstract string GetDropIndexSql(string tableName, string indexName);

        public virtual bool IsIndexCustomStateEqual(IndexCustomState a, IndexCustomState b)
        {
            if (a != null) throw new NotImplementedException(a.CustomStateType + " is not a supported feature on " + DisplayName);
            if (b != null) throw new NotImplementedException(b.CustomStateType + " is not a supported feature on " + DisplayName);
            return true;
        }

        #endregion

        #region Foreign keys

        // Add fkey (not currently different based on DBs)
        public virtual string GetAddFkeySql(string fromTableName, string toTableName, string fkeyName, bool isCascadeDelete, IEnumerable<FieldPair> fieldPairs)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(fromTableName) +
                " ADD CONSTRAINT " + QuoteIdentifier(fkeyName) + " FOREIGN KEY (" +
                string.Join(", ", from field in fieldPairs select QuoteIdentifier(field.FromFieldName)) +
                ") REFERENCES " + QuoteSchemaIdentifier(toTableName) + " (" +
                string.Join(", ", from field in fieldPairs select QuoteIdentifier(field.ToFieldName)) +
                ")" + (isCascadeDelete ? " ON DELETE CASCADE" : "");
        }

        // Drop fkey (not currently different based on DBs)
        public virtual string GetDropFkeySql(string tableName, string fkeyName)
        {
            return "ALTER TABLE " + QuoteSchemaIdentifier(tableName) + " DROP CONSTRAINT " + QuoteIdentifier(fkeyName);
        }

        #endregion

        #region Triggers

        public virtual string ExtractTriggerBody(string fullSql)
        {
            return ExtractProcOrFunctionBody(fullSql);
        }

        // Add Trigger
        public virtual string GetCreateTriggerSql(string tableName, string triggerName, TriggerTiming timing, TriggerEvents events, string body)
        {
            return "CREATE TRIGGER " + QuoteSchemaIdentifier(triggerName) + " ON " + QuoteSchemaIdentifier(tableName) + " " + timing.ToSql() + " " +
                events.ToSql() + " AS BEGIN\r\n" + body + "\r\nEND;\r\n";
        }

        // Drop trigger
        public virtual string GetDropTriggerSql(string tableName, string triggerName)
        {
            return "DROP TRIGGER " + QuoteSchemaIdentifier(triggerName);
        }

        #endregion

        #region Stored procedures and functions

        // Whether CREATE OR REPLACE PROCEDURE is supported
        public virtual bool IsCreateOrReplaceProcSupported { get { return true; } }

        // Override if procedure/function parameters need to be identified
        // (eg with an @ prefix)
        public virtual string QuoteParam(string name)
        {
            return name;
        }

        public virtual string UnquoteParam(string name)
        {
            return name;
        }

        public virtual string GetCreateProcSql(string procName, IEnumerable<ProcParam> parameters, string body)
        {
            return "CREATE " + (IsCreateOrReplaceProcSupported ? "OR REPLACE " : "") +
                "PROCEDURE " + QuoteSchemaIdentifier(procName) + " " +
                (!parameters.Any() ? "" :
                    "(" + string.Join(",", from param in parameters select "\r\n  " + QuoteParam(param.Name) + " " + param.DataType) + ")") +
                " AS BEGIN\r\n" +
                body +
                "\r\nEND;\r\n";
        }

        public virtual string GetDropProcSql(string procName)
        {
            return "DROP PROCEDURE " + QuoteSchemaIdentifier(procName);
        }

        public virtual string GetExecuteProcSql(string procName)
        {
            return "CALL " + QuoteSchemaIdentifier(procName);
        }

        public virtual string ExtractProcBody(string fullSql)
        {
            return ExtractProcOrFunctionBody(fullSql);
        }

        public virtual string GetCreateFunctionSql(string functionName, IEnumerable<ProcParam> parameters, string returnType, string body)
        {
            return "CREATE " + (IsCreateOrReplaceProcSupported ? "OR REPLACE " : "") +
                "FUNCTION " + QuoteSchemaIdentifier(functionName) + " " +
                (!parameters.Any() ? "" :
                    "(" + string.Join(",", from param in parameters select "\r\n  " + QuoteParam(param.Name) + " " + param.DataType) + ")") +
                "\r\nRETURNS " + returnType + " AS BEGIN\r\n" +
                body +
                "\r\nEND;\r\n";
        }

        public virtual string GetDropFunctionSql(string functionName)
        {
            return "DROP FUNCTION " + QuoteSchemaIdentifier(functionName);
        }

        public virtual string ExtractFunctionBody(string fullSql)
        {
            return ExtractProcOrFunctionBody(fullSql);
        }

        public virtual string ExtractProcOrFunctionBody(string fullSql)
        {
            // Ideally we would use some kind of proper parser here, but in lieu of one of those we just do something that gets roughly the right result

            var match = Regex.Match(fullSql, @"^
                \s*
                CREATE \s+
                (OR \s+ REPLACE \s+)?
                ( (
                    (FUNCTION | PROC(EDURE)?) \s+ " + SchemaIdentifierRegex + @" \s*
                      ( \( \s* " + ParamNameRegex + @" \s+ " + DataTypeRegex + @" \s*
                        ( , \s* " + ParamNameRegex + @" \s+ \w+ ( \s* \( \s* \w+ (, \s* \w+)? \s* \) )? \s* )* \) )? \s+
                      (RETURNS \s+ " + DataTypeRegex + @" \s+)?
                ) | (
                    TRIGGER \s+ " + SchemaIdentifierRegex + @" \s+ ON \s+ " + SchemaIdentifierRegex + @" \s+
                    (FOR | AFTER | INSTEAD \s+ OF) \s+
                    (INSERT | UPDATE | DELETE) ( \s* , \s* (INSERT | UPDATE | DELETE) ){0, 2} \s+
                ) | VIEW ) 
                AS \s+
                ( (BEGIN \s* (?<body1>.*?) \s* END ;?) | (?<body2>.*?) ) \s*
                $", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

            if (!match.Success) return "<FAIL>Failed to parse proc/function body " + fullSql.Substring(0, Math.Min(50, fullSql.Length)) + "...";

            return match.Groups["body1"].Success ? match.Groups["body1"].Value : match.Groups["body2"].Value;
        }

        public virtual string DataTypeRegex { get { return @"\w+ ( \s* \( \s* \w+ (, \s* \w+)? \s* \) )?"; } }
        public virtual string SchemaIdentifierRegex { get { return @"(" + IdentifierRegex + @"\. )?" + IdentifierRegex; } }
        public virtual string ParamNameRegex { get { return @"\w+"; } }
        public virtual string IdentifierRegex { get { return @"( \w+ | ("" [^""]+ ""))"; } }

        #endregion

        #region Views

        public virtual string GetCreateViewSql(string viewName, string body)
        {
            return "CREATE VIEW " + QuoteSchemaIdentifier(viewName) + " AS \r\n" + body + "\r\n";
        }

        public virtual string GetDropViewSql(string viewName)
        {
            return "DROP VIEW " + QuoteSchemaIdentifier(viewName);
        }

        public virtual string ExtractViewBody(string fullSql)
        {
            return ExtractProcOrFunctionBody(fullSql);
        }

        #endregion

        #region Fulltext indexes

        public virtual bool IsFulltextSupported(NrdoConnection connection)
        {
            return IsFulltextIndexSupported;
        }

        public virtual bool IsFulltextCatalogUsed { get { return false; } }
        public virtual bool IsFulltextIndexSupported { get { return false; } }

        public virtual string GetEnableFulltextSql(NrdoConnection connection) { return null; }

        public virtual string GetCreateFulltextCatalogSql(string name)
        {
            if (IsFulltextCatalogUsed) throw new NotImplementedException("Create fulltext catalog not implemented for " + DisplayName);
            return null;
        }

        public virtual string GetDropFulltextCatalogSql(string name)
        {
            if (IsFulltextCatalogUsed) throw new NotImplementedException("Drop fulltext catalog not implemented for " + DisplayName);
            return null;
        }

        public virtual string GetCreateFulltextIndexSql(string tableName, string catalogName, string keyName, IEnumerable<string> columns)
        {
            if (IsFulltextIndexSupported) throw new NotImplementedException("Create fulltext index not implemented for " + DisplayName);
            return null;
        }

        public virtual string GetDropFulltextIndexSql(string tableName)
        {
            if (IsFulltextIndexSupported) throw new NotImplementedException("Drop fulltext index not implemented for " + DisplayName);
            return null;
        }

        #endregion

        #region Database-specific quirks

        // Whether to execute a 200ms sleep after executing an SQL statement
        public virtual bool IsSleepRequiredBetweenStatements { get { return false; } }

        // When writing SQL to a file, separate by this
        public virtual string CommandSeparator { get { return null; } }

        #endregion

        #region SQL syntax substitution

        public abstract string GetDeclareSql(string varname, string vartype);

        public virtual string GetDeclareEndBlockSql(string varname)
        {
            return "";
        }

        public abstract string GetVariableValueSql(string varname);

        public abstract string GetAssignBeginSql(string varname);

        public virtual string GetAssignEndSql(string varname)
        {
            return "";
        }

        #endregion

        #region Helper wrappers for more convenient direct access to DbDriver methods

        protected bool StringEquals(string a, string b)
        {
            return DbDriver.StringEquals(a, b);
        }

        protected string ExtractSchema(ref string name)
        {
            return DbDriver.ExtractSchema(ref name);
        }

        // How are identifiers quoted
        protected string QuoteIdentifier(string identifier)
        {
            return DbDriver.QuoteIdentifier(identifier);
        }

        protected string QuoteSchemaIdentifier(string name)
        {
            return DbDriver.QuoteSchemaIdentifier(name);
        }

        #endregion

        #region Introspecting data types

        public virtual string GetDataTypeName(string type, int? charMaxLength, byte? numPrecision, int? numScale)
        {
            if (charMaxLength != null)
            {
                if (numPrecision != null || numScale != null) throw new ArgumentException("Cannot format name of a type (" + type + ") that includes both character length and numeric precision");

                var lengthString = GetTypeLengthString(type, (int)charMaxLength);
                if (lengthString != null) return type + "(" + lengthString + ")";
            }
            else if (numPrecision != null)
            {
                var precisionString = GetTypePrecisionString(type, (byte)numPrecision, numScale);
                if (precisionString != null) return type + "(" + precisionString + ")";
            }
            else if (numScale != null)
            {
                throw new ArgumentException("Cannot format name of a type (" + type + ") that includes numeric scale without precision");
            }

            return type;
        }

        public virtual string GetTypePrecisionString(string type, byte precision, int? scale)
        {
            if (scale != null && TypeIncludesPrecisionAndScale(type, precision, (int)scale))
            {
                return precision + "," + scale;
            }
            else if (TypeIncludesPrecision(type, precision))
            {
                return precision.ToString();
            }
            else
            {
                return null;
            }
        }

        public virtual bool TypeIncludesPrecisionAndScale(string type, byte precision, int scale)
        {
            return scale > 0;
        }

        public virtual bool TypeIncludesPrecision(string type, byte precision)
        {
            return false;
        }

        public virtual string GetTypeLengthString(string type, int length)
        {
            if (TypeIncludesLength(type, length))
            {
                return length < 0 ? "MAX" : length.ToString();
            }
            else
            {
                return null;
            }
        }

        public virtual bool TypeIncludesLength(string type, int length)
        {
            return true;
        }
        
        #endregion

        #region Introspection

        public virtual IEnumerable<IntrospectedTable> GetAllTables(NrdoConnection connection)
        {
            return connection.ExecuteSql("select t.table_schema as t_schema, t.table_name as t_name from information_schema.tables t where t.table_type = 'BASE TABLE'",
                result => new IntrospectedTable(result.GetString("t_schema"), result.GetString("t_name")));
        }

        public virtual IEnumerable<IntrospectedField> GetAllFields(NrdoConnection connection)
        {
            string isIdentitySelectSql;
            string isIdentityFromSql;
            string isIdentityWhereSql;
            Func<NrdoResult, bool> getIdentity;
            Func<NrdoResult, string> getSequenceName;
            GetInformationSchemaFieldIsSequencedSql("c", out isIdentitySelectSql, out isIdentityFromSql, out isIdentityWhereSql, out getIdentity, out getSequenceName);

            return connection.ExecuteSql("select c.table_schema as t_schema, c.table_name as t_name, c.column_name as c_name, c.is_nullable as c_nullable, " +
                                         "c.ordinal_position as c_pos, c.data_type as c_type, c.character_maximum_length as c_length, " +
                                         "c.numeric_precision as c_precision, c.numeric_scale as c_scale" + isIdentitySelectSql +
                                         " from information_schema.tables t, information_schema.columns c" + isIdentityFromSql +
                                         " where t.table_schema = c.table_schema and t.table_name = c.table_name and t.table_type = 'BASE TABLE'" + isIdentityWhereSql,
                                         result => new IntrospectedField(new IntrospectedTable(result.GetString("t_schema"), result.GetString("t_name")),
                                             result.GetString("c_name"),
                                             (int)result.GetInt("c_pos"),
                                             GetDataTypeName(result.GetString("c_type"), result.GetInt("c_length"), result.GetByte("c_precision"), result.GetInt("c_scale")),
                                             GetInformationSchemaFieldNullability(result.GetString("c_nullable")),
                                             getIdentity(result),
                                             getSequenceName(result)));
        }

        protected virtual bool GetInformationSchemaFieldNullability(string isNullableColumnValue)
        {
            if (StringEquals("YES", isNullableColumnValue)) return true;
            if (StringEquals("NO", isNullableColumnValue)) return false;
            throw new ArgumentException("Unexpected value '" + isNullableColumnValue + "' in information_schema.columns.is_nullable - expected YES or NO");
        }

        protected virtual void GetInformationSchemaFieldIsSequencedSql(string informationSchemaColumnsTableAlias,
            out string selectClause, out string fromClause, out string whereClause, out Func<NrdoResult, bool> getResult, out Func<NrdoResult, string> getSequenceName)
        {
            if (IsSequencedPartOfFieldDeclaration) throw new NotImplementedException("Determining whether field is a sequenced primary key not supported on " + DisplayName);

            selectClause = null;
            fromClause = null;
            whereClause = null;
            getResult = result => false;
            getSequenceName = result => null;
        }

        public virtual IEnumerable<IntrospectedIndex> GetAllUniqueIndexes(NrdoConnection connection)
        {
            var uniqueIndexColumns = connection.ExecuteSql("select k.constraint_schema as k_schema, k.constraint_name as k_name, k.table_schema as t_schema, k.table_name as t_name, " +
                                                           "k.constraint_type as k_type, u.column_name as c_name, u.ordinal_position as c_index " +
                                                           "from information_schema.table_constraints k, information_schema.key_column_usage u " +
                                                           "where (k.constraint_type = 'PRIMARY KEY' or k.constraint_type = 'UNIQUE') " +
                                                           "and k.constraint_schema = u.constraint_schema and k.constraint_name = u.constraint_name " +
                                                           "and k.table_schema = u.table_schema and k.table_name = u.table_name ",
                                                           result => new
                                                           {
                                                               keySchema = result.GetString("k_schema"),
                                                               keyName = result.GetString("k_name"),
                                                               tableSchema = result.GetString("t_schema"),
                                                               tableName = result.GetString("t_name"),
                                                               keyType = result.GetString("k_type"),
                                                               columnName = result.GetString("c_name"),
                                                               columnIndex = result.GetInt("c_index"),
                                                           });
            return from col in uniqueIndexColumns
                   group col by new { col.keySchema, col.keyName, col.tableSchema, col.tableName, col.keyType } into cols
                   select new IntrospectedIndex(
                       new IntrospectedTable(cols.Key.tableSchema, cols.Key.tableName),
                       StringEquals(cols.Key.keyType, "PRIMARY KEY"), true, cols.Key.keyName,
                       from col in cols orderby col.columnIndex select col.columnName);
        }

        // INFORMATION_SCHEMA does not include a way to get at non-unique indexes
        public abstract IEnumerable<IntrospectedIndex> GetAllNonUniqueIndexes(NrdoConnection connection);

        // It's slightly hacky to have core nrdo support for SQL server specific features, but figuring out a general extensibility mechanism
        // would be more work than there's time for for now, so these methods pick up clustering and included-field information about indexes
        // on "any database that happens to support it" - which is to say, SQL server.
        public virtual IEnumerable<IntrospectedIndexCustomState> GetAllIndexCustomState(NrdoConnection connection)
        {
            yield break;
        }

        public virtual IEnumerable<IntrospectedForeignKey> GetAllForeignKeys(NrdoConnection connection)
        {
            var fkeyJoins = connection.ExecuteSql("select from_k.table_schema as from_t_schema, from_k.table_name as from_t_name, " +
                                                  "to_k.table_schema as to_t_schema, to_k.table_name as to_t_name, " +
                                                  "f.constraint_name as f_name, f.delete_rule as f_rule, " +
                                                  "from_u.column_name as from_c_name, to_u.column_name as to_c_name, " +
                                                  "from_u.ordinal_position as c_index " +
                                                  "from information_schema.referential_constraints f, " +
                                                  "information_schema.table_constraints from_k, information_schema.table_constraints to_k, " +
                                                  "information_schema.key_column_usage from_u, information_schema.key_column_usage to_u " +
                                                  "where f.constraint_schema = from_k.constraint_schema and f.constraint_name = from_k.constraint_name " +
                                                  "and f.unique_constraint_schema = to_k.constraint_schema and f.unique_constraint_name = to_k.constraint_name " +
                                                  "and f.constraint_schema = from_u.constraint_schema and f.constraint_name = from_u.constraint_name " +
                                                  "and f.unique_constraint_schema = to_u.constraint_schema and f.unique_constraint_name = to_u.constraint_name " +
                                                  "and from_u.ordinal_position = to_u.ordinal_position",
                                                  result => new
                                                  {
                                                      fromSchema = result.GetString("from_t_schema"),
                                                      fromTableName = result.GetString("from_t_name"),
                                                      toSchema = result.GetString("to_t_schema"),
                                                      toTableName = result.GetString("to_t_name"),
                                                      fkeyName = result.GetString("f_name"),
                                                      deleteRule = result.GetString("f_rule"),
                                                      fromColumn = result.GetString("from_c_name"),
                                                      toColumn = result.GetString("to_c_name"),
                                                      columnIndex = result.GetInt("c_index"),
                                                  });

            return from j in fkeyJoins
                   group j by new { j.fromSchema, j.fromTableName, j.toSchema, j.toTableName, j.fkeyName, j.deleteRule } into joins
                   select new IntrospectedForeignKey(
                       new IntrospectedTable(joins.Key.fromSchema, joins.Key.fromTableName),
                       new IntrospectedTable(joins.Key.toSchema, joins.Key.toTableName),
                       joins.Key.fkeyName, StringEquals(joins.Key.deleteRule, "CASCADE"),
                       from fieldPair in joins orderby fieldPair.columnIndex select new FieldPair(fieldPair.fromColumn, fieldPair.toColumn));
        }

        public virtual IEnumerable<IntrospectedSequence> GetAllSequences(NrdoConnection connection)
        {
            return connection.ExecuteSql("select s.sequence_schema as s_schema, s.sequence_name as s_name from information_schema.sequences s",
                result => new IntrospectedSequence(result.GetString("s_schema"), result.GetString("s_name")));
        }

        // INFORMATION_SCHEMA does not provide a standard way to get triggers.
        public abstract IEnumerable<IntrospectedTrigger> GetAllTriggers(NrdoConnection connection);

        public virtual IEnumerable<IntrospectedProc> GetAllStoredProcsAndFunctions(NrdoConnection connection)
        {
            var routines = connection.ExecuteSql("select r.specific_schema as s_schema, r.specific_name as s_name, " + // Doesn't seem to be any difference between specific_xyz and routine_xyz but just in case...
                                                 "r.routine_schema as r_schema, r.routine_name as r_name, " +
                                                 "r.routine_type as r_type, " +
                                                 "r.data_type as r_datatype, r.character_maximum_length as r_length, " +
                                                 "r.numeric_precision as r_precision, r.numeric_scale as r_scale, " +
                                                 "r.routine_definition as r_definition " +
                                                 "from information_schema.routines r " +
                                                 "where (r.routine_type = 'PROCEDURE' or r.routine_type = 'FUNCTION') " +
                                                 "and r.routine_body = 'SQL'",
                                                 result => new
                                                 {
                                                     specSchema = result.GetString("s_schema"),
                                                     specName = result.GetString("s_name"),
                                                     schema = result.GetString("r_schema"),
                                                     name = result.GetString("r_name"),
                                                     type = result.GetString("r_type"),
                                                     dataType = result.GetString("r_datatype") == null ? null :
                                                        GetDataTypeName(result.GetString("r_datatype"), result.GetInt("r_length"), result.GetByte("r_precision"), result.GetInt("r_scale")),
                                                     definition = result.GetString("r_definition"),
                                                 });

            var parameters = connection.ExecuteSql("select p.specific_schema as s_schema, p.specific_name as s_name, p.ordinal_position as p_index, " +
                                                   "p.parameter_name as p_name, p.data_type as p_type, p.character_maximum_length as p_length, " +
                                                   "p.numeric_precision as p_precision, p.numeric_scale as p_scale " +
                                                   "from information_schema.parameters p " +
                                                   "where p.is_result = 'NO'",
                                                   result => new
                                                   {
                                                       specSchema = result.GetString("s_schema"),
                                                       specName = result.GetString("s_name"),
                                                       name = UnquoteParam(result.GetString("p_name")),
                                                       index = result.GetInt("p_index"),
                                                       dataType = GetDataTypeName(result.GetString("p_type"), result.GetInt("p_length"), result.GetByte("p_precision"), result.GetInt("p_scale")),
                                                   });

            return from r in routines
                   join p in parameters on new { r.specSchema, r.specName } equals new { p.specSchema, p.specName } into rparams
                   let procParams = from param in rparams orderby param.index select new ProcParam(param.name, param.dataType)
                   select StringEquals(r.type, "FUNCTION") ?
                       new IntrospectedFunction(r.schema, r.name, procParams, r.dataType, ExtractFunctionBody(r.definition)) :
                       new IntrospectedProc(r.schema, r.name, procParams, ExtractProcBody(r.definition));
        }

        public virtual IEnumerable<IntrospectedView> GetAllViews(NrdoConnection connection)
        {
            return connection.ExecuteSql("select t.table_schema as t_schema, t.table_name as t_name, t.view_definition as t_def from information_schema.views t",
                result => new IntrospectedView(result.GetString("t_schema"), result.GetString("t_name"), ExtractViewBody(result.GetString("t_def"))));
        }

        public virtual IEnumerable<string> GetAllFulltextCatalogs(NrdoConnection connection)
        {
            if (IsFulltextCatalogUsed) throw new NotImplementedException("Getting existing fulltext catalogs not implemented on " + DisplayName);
            yield break;
        }

        public virtual IEnumerable<IntrospectedFulltextIndex> GetAllFulltextIndexes(NrdoConnection connection)
        {
            if (IsFulltextIndexSupported) throw new NotImplementedException("Getting existing fulltext indexes not implemented on " + DisplayName);
            yield break;
        }

        #endregion

        #region Locking

        public virtual void TryAcquireSchemaUpdateLock(NrdoConnection connection)
        {
            // FIXME: the semantics of this method are easy to achieve on SQL server but may not be as easy to do on other database systems.
            // Finishing up support for other systems may require changing the API to something that's less convenient for the calling code.
            // The semantics of the method currently are:
            // - Only one connection can hold the lock at once
            // - Once a connection is holding the lock, any other connection trying to take it will fail with a SchemaLockFailException
            // - When the connection that's holding the lock is closed, the lock is automatically released
            throw new NotImplementedException("Schema lock not implemented on " + DisplayName);
        }

        protected internal class SchemaLockFailException : Exception
        {
        }

        #endregion
    }
}
