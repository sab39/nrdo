using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers.Introspection;
using System.Data;
using NR.nrdo.Schema.PortableSql;
using NR.nrdo.Schema.Tool;
using System.Threading;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Drivers
{
    public class SchemaConnection : NrdoConnection
    {
        private readonly SchemaDriver schemaDriver;
        public SchemaDriver SchemaDriver { get { return schemaDriver; } }

        private readonly Lazy<SqlTranslator> sqlTranslator;

        protected SchemaConnection(SchemaDriver schemaDriver, string connectionString)
            : base(schemaDriver.DbDriver, connectionString)
        {
            this.schemaDriver = schemaDriver;
            this.sqlTranslator = new Lazy<SqlTranslator>(() => new SqlTranslator(schemaDriver, null));
        }

        public static SchemaConnection Create(SchemaDriver schemaDriver, string connectionString)
        {
            return new SchemaConnection(schemaDriver, connectionString);
        }

        public void AcquireSchemaUpdateLock(IOutput output, TimeSpan timeout)
        {
            var deadline = DateTime.Now + timeout;
            output.Verbose("Acquiring schema update lock...");
            bool warned = false;
            while (true)
            {
                try
                {
                    schemaDriver.TryAcquireSchemaUpdateLock(this);
                    return;
                }
                catch (SchemaDriver.SchemaLockFailException)
                {
                    if (timeout == TimeSpan.Zero || DateTime.Now > deadline)
                    {
                        output.Error("Could not acquire schema update lock" + (timeout == TimeSpan.Zero ? "" : " after " + (int)timeout.TotalSeconds + " seconds") + "!");
                        throw new ApplicationException("Could not acquire schema update lock!");
                    }
                    if (!warned)
                    {
                        output.Warning("Waiting for schema update lock...");
                        warned = true;
                    }
                    Thread.Sleep(1000);
                }
            }
        }

        public void ExecutePortableSql(string sql, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            ExecuteSql(sqlTranslator.Value.Translate(sql), setParams, commandType);
        }

        public IEnumerable<T> ExecutePortableSql<T>(string sql, Func<NrdoResult, T> getResult, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            return ExecuteSql(sqlTranslator.Value.Translate(sql), getResult, setParams, commandType);
        }

        public IEnumerable<IntrospectedTable> GetAllTables()
        {
            return SchemaDriver.GetAllTables(this);
        }

        public IEnumerable<IntrospectedField> GetAllFields()
        {
            return SchemaDriver.GetAllFields(this);
        }

        public IEnumerable<IntrospectedIndex> GetAllUniqueIndexes()
        {
            return SchemaDriver.GetAllUniqueIndexes(this);
        }

        public IEnumerable<IntrospectedIndex> GetAllNonUniqueIndexes()
        {
            return SchemaDriver.GetAllNonUniqueIndexes(this);
        }

        public IEnumerable<IntrospectedIndexCustomState> GetAllIndexCustomState()
        {
            return SchemaDriver.GetAllIndexCustomState(this);
        }

        public IEnumerable<string> GetAllFulltextCatalogs()
        {
            return SchemaDriver.GetAllFulltextCatalogs(this);
        }

        public IEnumerable<IntrospectedFulltextIndex> GetAllFulltextIndexes()
        {
            return SchemaDriver.GetAllFulltextIndexes(this);
        }

        public IEnumerable<IntrospectedForeignKey> GetAllForeignKeys()
        {
            return SchemaDriver.GetAllForeignKeys(this);
        }

        public IEnumerable<IntrospectedSequence> GetAllSequences()
        {
            return SchemaDriver.GetAllSequences(this);
        }

        public IEnumerable<IntrospectedTrigger> GetAllTriggers()
        {
            return SchemaDriver.GetAllTriggers(this);
        }

        public IEnumerable<IntrospectedProc> GetAllStoredProcsAndFunctions()
        {
            return SchemaDriver.GetAllStoredProcsAndFunctions(this);
        }

        public IEnumerable<IntrospectedView> GetAllViews()
        {
            return SchemaDriver.GetAllViews(this);
        }
    }
}
