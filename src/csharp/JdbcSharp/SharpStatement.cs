using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using java.sql;
using java.util;

namespace JdbcSharp
{
    public class SharpStatement : Statement
    {
        #region Actual implemented stuff

        private SharpConnection conn;
        internal SharpStatement(SharpConnection conn)
        {
            this.conn = conn;
        }

        public void close()
        {
            // noop
        }

        public int executeUpdate(string sql)
        {
            try
            {
                using (DbCommand cmd = conn.factory.CreateCommand())
                {
                    cmd.Connection = conn.conn;
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = sql;
                    cmd.CommandTimeout = 60;
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (DbException e)
            {
                throw new SQLException(e.Message).initCause(e);
            }
        }

        #endregion

        #region Stubbed out methods

        public void addBatch(string str)
        {
            throw new NotImplementedException();
        }

        public void cancel()
        {
            throw new NotImplementedException();
        }

        public void clearBatch()
        {
            throw new NotImplementedException();
        }

        public void clearWarnings()
        {
            throw new NotImplementedException();
        }

        public bool execute(string str, string[] strarr)
        {
            throw new NotImplementedException();
        }

        public bool execute(string str, int[] iarr)
        {
            throw new NotImplementedException();
        }

        public bool execute(string str, int i)
        {
            throw new NotImplementedException();
        }

        public bool execute(string str)
        {
            throw new NotImplementedException();
        }

        public int[] executeBatch()
        {
            throw new NotImplementedException();
        }

        public ResultSet executeQuery(string str)
        {
            throw new NotImplementedException();
        }

        public int executeUpdate(string str, string[] strarr)
        {
            throw new NotImplementedException();
        }

        public int executeUpdate(string str, int[] iarr)
        {
            throw new NotImplementedException();
        }

        public int executeUpdate(string str, int i)
        {
            throw new NotImplementedException();
        }

        public Connection getConnection()
        {
            throw new NotImplementedException();
        }

        public int getFetchDirection()
        {
            throw new NotImplementedException();
        }

        public int getFetchSize()
        {
            throw new NotImplementedException();
        }

        public ResultSet getGeneratedKeys()
        {
            throw new NotImplementedException();
        }

        public int getMaxFieldSize()
        {
            throw new NotImplementedException();
        }

        public int getMaxRows()
        {
            throw new NotImplementedException();
        }

        public bool getMoreResults(int i)
        {
            throw new NotImplementedException();
        }

        public bool getMoreResults()
        {
            throw new NotImplementedException();
        }

        public int getQueryTimeout()
        {
            throw new NotImplementedException();
        }

        public ResultSet getResultSet()
        {
            throw new NotImplementedException();
        }

        public int getResultSetConcurrency()
        {
            throw new NotImplementedException();
        }

        public int getResultSetHoldability()
        {
            throw new NotImplementedException();
        }

        public int getResultSetType()
        {
            throw new NotImplementedException();
        }

        public int getUpdateCount()
        {
            throw new NotImplementedException();
        }

        public SQLWarning getWarnings()
        {
            throw new NotImplementedException();
        }

        public bool isClosed()
        {
            throw new NotImplementedException();
        }

        public bool isPoolable()
        {
            throw new NotImplementedException();
        }

        public void setCursorName(string str)
        {
            throw new NotImplementedException();
        }

        public void setEscapeProcessing(bool b)
        {
            throw new NotImplementedException();
        }

        public void setFetchDirection(int i)
        {
            throw new NotImplementedException();
        }

        public void setFetchSize(int i)
        {
            throw new NotImplementedException();
        }

        public void setMaxFieldSize(int i)
        {
            throw new NotImplementedException();
        }

        public void setMaxRows(int i)
        {
            throw new NotImplementedException();
        }

        public void setPoolable(bool b)
        {
            throw new NotImplementedException();
        }

        public void setQueryTimeout(int i)
        {
            throw new NotImplementedException();
        }

        public bool isWrapperFor(java.lang.Class c)
        {
            throw new NotImplementedException();
        }

        public object unwrap(java.lang.Class c)
        {
            throw new NotImplementedException();
        }

        public void closeOnCompletion()
        {
            throw new NotImplementedException();
        }

        public long[] executeLargeBatch()
        {
            throw new NotImplementedException();
        }

        public long executeLargeUpdate(string sql, string[] columnNames)
        {
            throw new NotImplementedException();
        }

        public long executeLargeUpdate(string sql, int[] columnIndexes)
        {
            throw new NotImplementedException();
        }

        public long executeLargeUpdate(string sql, int autoGeneratedKeys)
        {
            throw new NotImplementedException();
        }

        public long executeLargeUpdate(string sql)
        {
            throw new NotImplementedException();
        }

        public long getLargeMaxRows()
        {
            throw new NotImplementedException();
        }

        public long getLargeUpdateCount()
        {
            throw new NotImplementedException();
        }

        public bool isCloseOnCompletion()
        {
            throw new NotImplementedException();
        }

        public void setLargeMaxRows(long max)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
