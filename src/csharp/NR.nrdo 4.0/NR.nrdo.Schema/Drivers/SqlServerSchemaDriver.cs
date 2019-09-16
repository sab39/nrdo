using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers.Introspection;
using System.Data.Common;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers
{
    public class SqlServerSchemaDriver : SchemaDriver
    {
        public override DbDriver DbDriver { get { return SqlServerDriver.Instance; } }

        public int GetSqlServerMajorVersion(NrdoConnection connection)
        {
            // This combination of ServerProperty and ParseName extracts the major number from the sql server version
            return (int)connection.ExecuteSql("SELECT Cast(ParseName(Cast(ServerProperty('ProductVersion') As nvarchar), 4) As int) AS [sql_version]",
                                     result => result.GetInt("sql_version")).Single();
        }
        public override void TryAcquireSchemaUpdateLock(NrdoConnection connection)
        {
            try
            {
                connection.ExecuteSql(GetCreateTableSql("##nrdo_update_in_progress", new[] { new FieldCreation("nothing", "int", true, false, null) }));
            }
            catch (DbException)
            {
                throw new SchemaLockFailException();
            }
        }

        // How identity fields differ from non-identity fields
        public override string GetSequencedFieldTypeSpec(string datatype, bool isNullable, string sequenceName)
        {
            if (isNullable) throw new ArgumentException("SQL Server does not support nullable identity fields");
            return datatype + " IDENTITY NOT NULL";
        }

        // Whether an identity field can be created as nullable and altered, or
        // whether it must be created non-null off the bat.
        public override bool IsAlterSequencedFieldSupported { get { return false; } }

        // Drop index
        public override string GetDropIndexSql(string tableName, string indexName)
        {
            return "DROP INDEX " + QuoteSchemaIdentifier(tableName) + "." + QuoteIdentifier(indexName);
        }

        // Specific SQL server custom features on indexes
        public override string GetCustomizedUniqueConstraintKeyword(bool isPrimaryKey, IndexCustomState customState)
        {
            var state = customState as SqlServerIndexCustomState;
            if (state == null) return base.GetCustomizedUniqueConstraintKeyword(isPrimaryKey, customState);
            return GetUniqueConstraintKeyword(isPrimaryKey) + " " + state.ClusteringKeyword;
        }

        public override string GetCustomizedIndexKeyword(IndexCustomState customState)
        {
            var state = customState as SqlServerIndexCustomState;
            if (state == null) return base.GetCustomizedIndexKeyword(customState);
            return state.ClusteringKeyword + " INDEX";
        }

        public override string GetCustomizedCreateIndexSuffix(IndexCustomState customState)
        {
            var state = customState as SqlServerIndexCustomState;
            if (state == null) return base.GetCustomizedCreateIndexSuffix(customState);
            if (!state.IncludedFields.Any()) return "";

            return " INCLUDE (" + string.Join(", ", from field in state.IncludedFields select QuoteIdentifier(field)) + ")";
        }

        public override IndexCustomState DefaultPrimaryKeyCustomState { get { return SqlServerIndexCustomState.Clustered; } }
        public override IndexCustomState DefaultUniqueConstraintCustomState { get { return SqlServerIndexCustomState.NonClustered; } }
        public override IndexCustomState DefaultIndexCustomState { get { return SqlServerIndexCustomState.NonClustered; } }

        public override bool IsIndexCustomStateEqual(IndexCustomState a, IndexCustomState b)
        {
            var sqlA = a as SqlServerIndexCustomState;
            var sqlB = b as SqlServerIndexCustomState;
            if (sqlA == null || sqlB == null) return base.IsIndexCustomStateEqual(a, b);

            return sqlA.IsClustered == sqlB.IsClustered &&
                sqlA.IncludedFields.SetEquals(sqlB.IncludedFields);
        }

        // Rename table
        // Hard to believe, but SQL Server apparently still doesn't support ALTER TABLE RENAME
        public override string GetRenameTableSql(string oldTableName, string newTableName)
        {
            ExtractSchema(ref newTableName);
            return "EXEC sp_rename '" + QuoteSchemaIdentifier(oldTableName) + "', '" + newTableName + "'";
        }

        // Create table from select statement
        public override string GetCreateTableAsSelectSql(string tableName, string selectClause, string fromClause)
        {
            return "SELECT " + selectClause + " INTO " + QuoteSchemaIdentifier(tableName) + " FROM " + fromClause;
        }

        public override bool IsFulltextCatalogUsed { get { return true; } }
        public override bool IsFulltextIndexSupported { get { return true; } }

        public override bool IsFulltextSupported(NrdoConnection connection)
        {
            return connection.ExecuteSql("SELECT fulltextserviceproperty('isfulltextinstalled') AS [fulltext_enabled]",
                                        result => result.GetInt("fulltext_enabled") == 1).Single();
        }

        public override string GetEnableFulltextSql(NrdoConnection connection)
        {
            return GetSqlServerMajorVersion(connection) <= 9 ? "EXEC sp_fulltext_database 'enable'" : null;
        }

        public override string GetCreateFulltextCatalogSql(string name)
        {
            return "CREATE FULLTEXT CATALOG " + name;
        }

        public override string GetDropFulltextCatalogSql(string name)
        {
            return "DROP FULLTEXT CATALOG " + name;
        }

        public override string GetCreateFulltextIndexSql(string tableName, string catalogName, string keyName, IEnumerable<string> columns)
        {
            return "CREATE FULLTEXT INDEX ON " + QuoteSchemaIdentifier(tableName) + " (" +
                string.Join(", ", from col in columns select QuoteIdentifier(col)) + ") KEY INDEX " + QuoteIdentifier(keyName) + " ON " + catalogName;
        }

        public override string GetDropFulltextIndexSql(string tableName)
        {
            return "DROP FULLTEXT INDEX ON " + QuoteSchemaIdentifier(tableName);
        }

        // Param char on storedprocs
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

        public override string GetExecuteProcSql(string procName)
        {
            return "EXEC " + QuoteSchemaIdentifier(procName);
        }

        // Whether CREATE OR REPLACE PROCEDURE is supported
        public override bool IsCreateOrReplaceProcSupported { get { return false; } }

        public override string GetDeclareSql(string varname, string vartype)
        {
            return "declare @" + varname + " " + vartype;
        }

        public override string GetVariableValueSql(string varname)
        {
            return "@" + varname;
        }

        public override string GetAssignBeginSql(string varname)
        {
            return "select @" + varname + " = ";
        }

        // Command separator in .sql files
        public override string CommandSeparator
        {
            get { return "\r\nGO\r\n"; }
        }

        public override bool TypeIncludesLength(string type, int length)
        {
            // The text, ntext and image types have a value in information_schema.columns.character_maximum_length but are entered without it
            if (StringEquals(type, "text") || StringEquals(type, "ntext") || StringEquals(type, "image")) return false;
            return base.TypeIncludesLength(type, length);
        }

        public override bool TypeIncludesPrecisionAndScale(string type, byte precision, int scale)
        {
            if (StringEquals(type, "money")) return false;

            return base.TypeIncludesPrecisionAndScale(type, precision, scale);
        }

        public override bool TypeIncludesPrecision(string type, byte precision)
        {
            if (StringEquals(type, "decimal") || StringEquals(type, "numeric"))
            {
                return precision != 18;
            }
            else if (StringEquals(type, "float"))
            {
                return precision != 53;
            }
            return base.TypeIncludesPrecision(type, precision);
        }

        protected override void GetInformationSchemaFieldIsSequencedSql(string informationSchemaColumnsTableAlias,
            out string selectClause, out string fromClause, out string whereClause, out Func<NrdoResult, bool> getResult, out Func<NrdoResult, string> getSequenceName)
        {
            selectClause = ", columnproperty(object_id('[' + " + informationSchemaColumnsTableAlias + ".table_schema + '].[' + " +
                informationSchemaColumnsTableAlias + ".table_name + ']'), " + informationSchemaColumnsTableAlias + ".column_name, 'IsIdentity') as c_identity";
            fromClause = null;
            whereClause = null;
            getResult = result => result.GetInt("c_identity") == 1;
            getSequenceName = result => null;
        }

        public override IEnumerable<IntrospectedIndex> GetAllNonUniqueIndexes(NrdoConnection connection)
        {
            var indexColumns = connection.ExecuteSql("select object_schema_name(i.object_id) as t_schema, object_name(i.object_id) as t_name, i.name as i_name, " +
                                                     "c.name as c_name, ic.key_ordinal as c_index " +
                                                     "from sys.indexes i, information_schema.tables t, sys.index_columns ic, sys.columns c " +
                                                     "where i.is_unique = 0 and t.table_schema = object_schema_name(i.object_id) and t.table_name = object_name(i.object_id) " +
                                                     "and ic.is_included_column = 0 " +
                                                     "and ic.object_id = i.object_id and ic.index_id = i.index_id and ic.column_id = c.column_id and ic.object_id = c.object_id",
                                                     result => new
                                                     {
                                                         tableSchema = result.GetString("t_schema"),
                                                         tableName = result.GetString("t_name"),
                                                         indexName = result.GetString("i_name"),
                                                         columnName = result.GetString("c_name"),
                                                         columnIndex = result.GetByte("c_index"),
                                                     });

            return from col in indexColumns
                   group col by new { col.tableSchema, col.tableName, col.indexName } into cols
                   select new IntrospectedIndex(
                       new IntrospectedTable(cols.Key.tableSchema, cols.Key.tableName),
                       false, false, cols.Key.indexName,
                       from col in cols orderby col.columnIndex select col.columnName);
        }

        private bool getIsClustered(string typeDesc)
        {
            if (StringEquals(typeDesc, "CLUSTERED")) return true;
            if (StringEquals(typeDesc, "NONCLUSTERED")) return false;
            throw new ArgumentException("Unknown clusteredness: " + typeDesc);
        }

        public override IEnumerable<IntrospectedIndexCustomState> GetAllIndexCustomState(NrdoConnection connection)
        {
            var indexClustered = connection.ExecuteSql("select i.object_id as o_id, i.index_id as i_id, object_schema_name(i.object_id) as t_schema, object_name(i.object_id) as t_name, i.name as i_name, " +
                                                       "type_desc as i_type from sys.indexes i " +
                                                       "where i.type_desc = 'NONCLUSTERED' or i.type_desc = 'CLUSTERED'",
                                                       result => new
                                                       {
                                                           objectId = result.GetInt("o_id"),
                                                           indexId = result.GetInt("i_id"),
                                                           tableSchema = result.GetString("t_schema"),
                                                           tableName = result.GetString("t_name"),
                                                           indexName = result.GetString("i_name"),
                                                           isClustered = getIsClustered(result.GetString("i_type")),
                                                       });

            var indexColumns = connection.ExecuteSql("select ic.object_id as o_id, ic.index_id as i_id, c.name as c_name " +
                                                     "from sys.index_columns ic, sys.columns c " +
                                                     "where ic.is_included_column = 1 and ic.column_id = c.column_id and ic.object_id = c.object_id",
                                                     result => new
                                                     {
                                                         objectId = result.GetInt("o_id"),
                                                         indexId = result.GetInt("i_id"),
                                                         columnName = result.GetString("c_name"),
                                                     });

            return from index in indexClustered
                   join col in indexColumns on new { index.objectId, index.indexId } equals new { col.objectId, col.indexId } into includedCols
                   select new IntrospectedIndexCustomState(
                       new IntrospectedTable(index.tableSchema, index.tableName),
                       index.indexName,
                       new SqlServerIndexCustomState(index.isClustered, from c in includedCols select c.columnName));
        }

        public override IEnumerable<string> GetAllFulltextCatalogs(NrdoConnection connection)
        {
            return connection.ExecuteSql("select name from sys.fulltext_catalogs", result => result.GetString("name"));
        }

        public override IEnumerable<IntrospectedFulltextIndex> GetAllFulltextIndexes(NrdoConnection connection)
        {
            var fulltextIndexes = connection.ExecuteSql(@"
                select f.object_id as o_id, object_schema_name(f.object_id) as t_schema, object_name(f.object_id) as t_name, i.name as i_name, c.name as c_name
	                from sys.fulltext_indexes f, sys.indexes i, sys.fulltext_catalogs c
	                where f.object_id = i.object_id and f.unique_index_id = i.index_id
		                and f.fulltext_catalog_id = c.fulltext_catalog_id",
                result => new
                {
                    objectId = result.GetInt("o_id"),
                    tableSchema = result.GetString("t_schema"),
                    tableName = result.GetString("t_name"),
                    keyName = result.GetString("i_name"),
                    catalogName = result.GetString("c_name"),
                });
            
            var columns = connection.ExecuteSql(@"
                select f.object_id as o_id, c.name as c_name
	                from sys.fulltext_index_columns f, sys.columns c
	                where f.object_id = c.object_id and f.column_id = c.column_id",
                result => new
                {
                    objectId = result.GetInt("o_id"),
                    columnName = result.GetString("c_name"),
                });

            return from i in fulltextIndexes
                   select new IntrospectedFulltextIndex(new IntrospectedTable(i.tableSchema, i.tableName), i.catalogName, i.keyName,
                                                        from c in columns where c.objectId == i.objectId select c.columnName);
        }

        public override IEnumerable<IntrospectedTrigger> GetAllTriggers(NrdoConnection connection)
        {
            var triggers = connection.ExecuteSql(@"
                select tr.object_id as o_id,
                       object_schema_name(tr.parent_id) as t_schema, object_name(tr.parent_id) as t_name,
                       object_schema_name(tr.object_id) as tr_schema, tr.name as tr_name,
                       object_definition(tr.object_id) as tr_definition, is_instead_of_trigger as tr_instead
                from sys.triggers tr",
                result => new
                {
                    objectId = result.GetInt("o_id"),
                    tableSchema = result.GetString("t_schema"),
                    tableName = result.GetString("t_name"),
                    triggerSchema = result.GetString("tr_schema"),
                    triggerName = result.GetString("tr_name"),
                    triggerDef = result.GetString("tr_definition"),
                    timing = (bool)result.GetBool("tr_instead") ? TriggerTiming.InsteadOf : TriggerTiming.After,
                });

            var events = connection.ExecuteSql("select e.object_id as o_id, e.type_desc as tr_event from sys.trigger_events e",
                result => new
                {
                    objectId = result.GetInt("o_id"),
                    evt = (TriggerEvents)Enum.Parse(typeof(TriggerEvents), result.GetString("tr_event"), true),
                });

            var triggerEvents = triggers.ToDictionary(tr => tr.objectId, tr => default(TriggerEvents));
            foreach (var triggerEvent in events)
            {
                triggerEvents[triggerEvent.objectId] |= triggerEvent.evt;
            }

            return from trigger in triggers
                   select new IntrospectedTrigger(trigger.triggerSchema, trigger.triggerName,
                       new IntrospectedTable(trigger.tableSchema, trigger.tableName),
                       trigger.timing, triggerEvents[trigger.objectId], ExtractTriggerBody(trigger.triggerDef));
        }
    }
}
