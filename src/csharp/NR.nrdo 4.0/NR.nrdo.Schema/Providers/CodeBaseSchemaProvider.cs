using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using NR.nrdo.Reflection;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Schema.PortableSql;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Schema.Objects.Fulltext;

namespace NR.nrdo.Schema.Providers
{
    public class CodeBaseSchemaProvider : ISchemaProvider
    {
        private readonly INrdoCodeBase codeBase;
        private readonly string schemaName;
        private readonly bool preserveColumnOrder;

        public CodeBaseSchemaProvider(INrdoCodeBase codeBase, bool preserveColumnOrder = false, string schemaName = "dbo")
        {
            this.codeBase = codeBase;
            this.schemaName = schemaName;
            this.preserveColumnOrder = preserveColumnOrder;
        }

        #region Name hash strings
        // This takes the name in "module:submodule:table_name" format and is case-sensitive
        // The algorithm is weird because it preserves backward compatibility with the java implementation
        // Which in turn relied on Java's hashcode algorithm for strings!
        private static string getNameHashString(string fullName)
        {
            var sb = new StringBuilder(10);
            int pos = 0;
            for (int i = 0; i >= 0; i = fullName.IndexOf(':', i))
            {
                if (i > 0) i++;
                sb.Append(fullName[i]);
                pos = i;
            }
            pos = fullName.IndexOf('_', pos) + 1;
            while (pos > 0)
            {
                sb.Append(fullName[pos]);
                pos = fullName.IndexOf('_', pos) + 1;
            }
            var hc = getJavaCompatibleStringHashCode(fullName);
            var sc = (short)(hc ^ hc >> 16);
            var hx = ((ushort)sc).ToString("x4");
            return sb.ToString();
        }
        private static int getJavaCompatibleStringHashCode(string str)
        {
            int result = 0;
            for (int i = 0; i < str.Length; i++)
            {
                result = 31 * result + str[i++];
            }
            return result;
        }
        #endregion

        public IEnumerable<ObjectType> GetObjectTypes(SchemaDriver schemaDriver)
        {
            yield return BeforeStatementType.Instance;
            yield return TableType.Instance;
            yield return TableRenameType.Instance;
            yield return FieldType.Instance;
            yield return UniqueIndexType.Instance;
            yield return NonUniqueIndexType.Instance;
            yield return FulltextCatalogType.Instance;
            yield return FulltextIndexType.Instance;
            yield return FkeyType.Instance;
            if (schemaDriver.IsSequenceUsed) yield return SequenceType.Instance;
            if (schemaDriver.IsTriggerUsedForSequence) yield return TriggerType.Instance;
            yield return QueryType.Instance;
            yield return PreUpgradeHookType.Instance;

            if (preserveColumnOrder) yield return FieldOrderSensitivityType.Instance;
        }

        public IEnumerable<ObjectState> GetDesiredState(SchemaConnection connection, IOutput output)
        {
            var schemaDriver = connection.SchemaDriver;
            var sqlTranslator = new SqlTranslator(schemaDriver, null);

            HashSet<string> catalogs = new HashSet<string>(schemaDriver.DbDriver.DbStringComparer);

            foreach (var nrdoTable in codeBase.AllTables)
            {
                if (nrdoTable.ExistingName != null)
                {
                    if (nrdoTable.BeforeStatements.Any()) throw new ApplicationException("Table " + nrdoTable.Name + " is 'existing' but has before statements, which is no longer supported");
                    continue;
                }

                output.Verbose("Loading table " + nrdoTable.DatabaseName + " from " + nrdoTable.Type.Name);
                var table = TableType.Create(schemaName + "." + nrdoTable.DatabaseName);
                yield return table;

                if (preserveColumnOrder) yield return FieldOrderSensitivityType.Create(table.Identifier);

                string sequencedPkeyFieldName = null;
                string sequenceName = null;
                if (nrdoTable.IsPkeySequenced)
                {
                    sequencedPkeyFieldName = nrdoTable.PkeyGet.Fields.Single().Name;
                    var sequenceNameBase = getNameHashString(nrdoTable.Name) + "_" + sequencedPkeyFieldName;

                    if (schemaDriver.IsSequenceUsed)
                    {
                        sequenceName = "sq_" + sequenceNameBase;
                        yield return SequenceType.Create(schemaName + "." + sequenceName);
                    }

                    if (schemaDriver.IsTriggerUsedForSequence)
                    {
                        var triggerName = "sqt_" + sequenceNameBase;
                        yield return TriggerType.Create(table.Identifier, triggerName, schemaDriver.GetSequencedFieldTriggerTiming(), TriggerEvents.Insert,
                            schemaDriver.GetSequencedFieldTriggerBody(table.Name, sequencedPkeyFieldName, sequenceName));
                    }
                }

                var fieldIndex = 0;
                foreach (var nrdoField in nrdoTable.Fields)
                {
                    if (nrdoField.Name == sequencedPkeyFieldName)
                    {
                        yield return FieldType.CreateSequencedPkey(table.Identifier, nrdoField.Name, fieldIndex++, nrdoField.DbType, nrdoField.IsNullable, sequenceName);
                    }
                    else
                    {
                        yield return FieldType.Create(table.Identifier, nrdoField.Name, fieldIndex++, nrdoField.DbType, nrdoField.IsNullable);
                    }
                }

                foreach (var nrdoIndex in nrdoTable.Indexes)
                {
                    if (nrdoIndex.IsPrimary)
                    {
                        yield return UniqueIndexType.CreatePrimaryKey(table.Identifier, nrdoIndex.Name,
                            from field in nrdoIndex.Fields select FieldType.Identifier(field), schemaDriver.DefaultPrimaryKeyCustomState);
                    }
                    else if (nrdoIndex.IsUnique)
                    {
                        yield return UniqueIndexType.CreateUnique(table.Identifier, nrdoIndex.Name,
                            from field in nrdoIndex.Fields select FieldType.Identifier(field), schemaDriver.DefaultUniqueConstraintCustomState);
                    }
                    else
                    {
                        yield return NonUniqueIndexType.Create(table.Identifier, nrdoIndex.Name,
                            from field in nrdoIndex.Fields select FieldType.Identifier(field), schemaDriver.DefaultIndexCustomState);
                    }
                }

                if (nrdoTable.FulltextFields.Any() && connection.SchemaDriver.IsFulltextSupported(connection))
                {
                    var catalog = nrdoTable.FulltextCatalog ?? "NrdoFulltext";
                    if (!catalogs.Contains(catalog))
                    {
                        yield return FulltextCatalogType.Create(catalog);
                        catalogs.Add(catalog);
                    }

                    var pkey = nrdoTable.Indexes.Where(index => index.IsPrimary).Single();
                    yield return FulltextIndexType.Create(table.Identifier, FulltextCatalogType.Identifier(catalog), pkey.Name, nrdoTable.FulltextFields);
                }

                foreach (var nrdoFkey in nrdoTable.References)
                {
                    if (nrdoFkey.IsFkey)
                    {
                        yield return FkeyType.Create(table.Identifier, TableType.Identifier(schemaName + "." + nrdoFkey.TargetTable.DatabaseName),
                            nrdoFkey.FkeyName, nrdoFkey.IsCascadingFkey,
                            from fjoin in nrdoFkey.Joins select new FieldPair(fjoin.From.Name, fjoin.To.Name));
                    }
                }

                var beforeIndex = 0;
                foreach (var nrdoBefore in nrdoTable.BeforeStatements)
                {
                    yield return BeforeStatementType.Create(table.Identifier, "0" + nrdoTable.Name, beforeIndex++, nrdoBefore.Name, nrdoBefore.Step,
                        nrdoBefore.Initial, nrdoBefore.Upgrade, sqlTranslator.Translate(nrdoBefore.Statement));
                }

                foreach (var oldName in nrdoTable.RenamedFrom)
                {
                    var oldDbName = schemaName + "." + oldName.Replace(':', '_');
                    yield return TableRenameType.Create(oldDbName, table.Name);
                }
            }

            foreach (var nrdoQuery in codeBase.AllQueries)
            {
                output.Verbose("Loading query " + nrdoQuery.DatabaseName + " from " + nrdoQuery.Type.Name);

                var queryName = schemaName + "." + nrdoQuery.DatabaseName;

                var parameters = from param in nrdoQuery.Params select new ProcParam(param.Name, param.DbType);

                if (nrdoQuery.IsStoredProc)
                {
                    yield return QueryType.CreateProc(queryName, parameters, sqlTranslator.Translate(nrdoQuery.Sql));

                    if (nrdoQuery.IsPreUpgradeHook) yield return PreUpgradeHookType.Create(queryName);
                }
                else if (nrdoQuery.IsStoredFunction)
                {
                    var returnType = nrdoQuery.Results.Single().DbType;
                    yield return QueryType.CreateFunction(queryName, parameters, returnType, sqlTranslator.Translate(nrdoQuery.Sql));
                }
                else
                {
                    yield return QueryType.CreateUnstored(queryName);
                }

                var beforeIndex = 0;
                foreach (var nrdoBefore in nrdoQuery.BeforeStatements)
                {
                    yield return BeforeStatementType.Create(QueryType.Identifier(queryName), "1" + nrdoQuery.Name, beforeIndex++, nrdoBefore.Name, nrdoBefore.Step,
                        nrdoBefore.Initial, nrdoBefore.Upgrade, sqlTranslator.Translate(nrdoBefore.Statement));
                }
            }
        }
    }
}
