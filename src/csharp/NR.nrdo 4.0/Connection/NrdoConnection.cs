using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public class NrdoConnection : IDisposable
    {
        private readonly DbDriver dbDriver;
        private readonly IDbConnection connection;

        public DbDriver DbDriver { get { return dbDriver; } }
        public IDbConnection Connection { get { return connection; } }

        protected NrdoConnection(DbDriver dbDriver, string connectionString)
        {
            this.dbDriver = dbDriver;
            this.connection = dbDriver.CreateConnection(connectionString);
            connection.Open();
        }

        public static NrdoConnection Create(DbDriver dbDriver, string connectionString)
        {
            return new NrdoConnection(dbDriver, connectionString);
        }

        public static NrdoConnection Create(DataBase db)
        {
            return Create(db.DbDriver, db.ConnectionString);
        }

        // FIXME: This code does not work nicely with NrdoTransactedScope at this time
        private NrdoTransaction currentTransaction;
        public bool IsTransactionActive { get { return currentTransaction != null && currentTransaction.IsActive; } }

        public NrdoTransaction StartTransaction()
        {
            if (IsTransactionActive) throw new InvalidOperationException("Can't start a transaction when one is already open");
            currentTransaction = new NrdoTransaction(connection);
            return currentTransaction;
        }

        public void ExecuteSql(string sql, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            executeSqlImpl(sql, setParams, commandType, cmd => cmd.ExecuteNonQuery());
        }

        public IEnumerable<T> ExecuteSql<T>(string sql, Func<NrdoResult, T> getResult, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            List<T> result = null;
            executeSqlImpl(sql, setParams, commandType, cmd =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    result = (from row in NrdoResult.Get(dbDriver, reader) select getResult(row)).ToList();
                }
            });
            return result;
        }

        private void executeSqlImpl(string sql, Action<NrdoCommand> setParams, CommandType commandType, Action<IDbCommand> execute)
        {
            using (var cmd = connection.CreateCommand())
            {
                if (IsTransactionActive) currentTransaction.ApplyToCommand(cmd);
                cmd.CommandType = commandType;
                cmd.CommandText = sql;
                if (setParams != null) setParams(new NrdoCommand(dbDriver, cmd));
                execute(cmd);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
            }
        }
    }
}
