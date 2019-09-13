using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using java.sql;
using java.util;

namespace JdbcSharp
{
    public class SharpConnection : Connection
    {
        #region Actual implemented stuff
        internal DbProviderFactory factory;
        internal DbConnection conn;

        internal SharpConnection(string url)
        {
            string urlbase = url.Substring("jdbc:sharp:".Length);
            int colon = urlbase.IndexOf(':');
            string provider = urlbase.Substring(0, colon);
            string connstr = urlbase.Substring(colon + 1);
            factory = DbProviderFactories.GetFactory(provider);
            conn = factory.CreateConnection();
            conn.ConnectionString = connstr;
            conn.Open();
        }

        public void close()
        {
            conn.Close();
        }

        public Statement createStatement()
        {
            return new SharpStatement(this);
        }

        public bool isClosed()
        {
            return conn.State == ConnectionState.Closed;
        }
        #endregion

        #region Stubbed out methods

        public void clearWarnings()
        {
            throw new NotImplementedException();
        }

        public void commit()
        {
            throw new NotImplementedException();
        }

        public java.sql.Array createArrayOf(string str, object[] objarr)
        {
            throw new NotImplementedException();
        }

        public Blob createBlob()
        {
            throw new NotImplementedException();
        }

        public Clob createClob()
        {
            throw new NotImplementedException();
        }

        public NClob createNClob()
        {
            throw new NotImplementedException();
        }

        public SQLXML createSQLXML()
        {
            throw new NotImplementedException();
        }

        public Statement createStatement(int i1, int i2, int i3)
        {
            throw new NotImplementedException();
        }

        public Statement createStatement(int i1, int i2)
        {
            throw new NotImplementedException();
        }

        public Struct createStruct(string str, object[] objarr)
        {
            throw new NotImplementedException();
        }

        public bool getAutoCommit()
        {
            throw new NotImplementedException();
        }

        public string getCatalog()
        {
            throw new NotImplementedException();
        }

        public Properties getClientInfo()
        {
            throw new NotImplementedException();
        }

        public string getClientInfo(string str)
        {
            throw new NotImplementedException();
        }

        public int getHoldability()
        {
            throw new NotImplementedException();
        }

        public DatabaseMetaData getMetaData()
        {
            throw new NotImplementedException();
        }

        public int getTransactionIsolation()
        {
            throw new NotImplementedException();
        }

        public Map getTypeMap()
        {
            throw new NotImplementedException();
        }

        public SQLWarning getWarnings()
        {
            throw new NotImplementedException();
        }

        public bool isReadOnly()
        {
            throw new NotImplementedException();
        }

        public bool isValid(int i)
        {
            throw new NotImplementedException();
        }

        public string nativeSQL(string str)
        {
            throw new NotImplementedException();
        }

        public CallableStatement prepareCall(string str, int i1, int i2, int i3)
        {
            throw new NotImplementedException();
        }

        public CallableStatement prepareCall(string str, int i1, int i2)
        {
            throw new NotImplementedException();
        }

        public CallableStatement prepareCall(string str)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str, string[] strarr)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str, int[] iarr)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str, int i)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str, int i1, int i2, int i3)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str, int i1, int i2)
        {
            throw new NotImplementedException();
        }

        public PreparedStatement prepareStatement(string str)
        {
            throw new NotImplementedException();
        }

        public void releaseSavepoint(Savepoint s)
        {
            throw new NotImplementedException();
        }

        public void rollback(Savepoint s)
        {
            throw new NotImplementedException();
        }

        public void rollback()
        {
            throw new NotImplementedException();
        }

        public void setAutoCommit(bool b)
        {
            throw new NotImplementedException();
        }

        public void setCatalog(string str)
        {
            throw new NotImplementedException();
        }

        public void setClientInfo(Properties p)
        {
            throw new NotImplementedException();
        }

        public void setClientInfo(string str1, string str2)
        {
            throw new NotImplementedException();
        }

        public void setHoldability(int i)
        {
            throw new NotImplementedException();
        }

        public void setReadOnly(bool b)
        {
            throw new NotImplementedException();
        }

        public Savepoint setSavepoint(string str)
        {
            throw new NotImplementedException();
        }

        public Savepoint setSavepoint()
        {
            throw new NotImplementedException();
        }

        public void setTransactionIsolation(int i)
        {
            throw new NotImplementedException();
        }

        public void setTypeMap(Map m)
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

        public void abort(java.util.concurrent.Executor executor)
        {
            throw new NotImplementedException();
        }

        public int getNetworkTimeout()
        {
            throw new NotImplementedException();
        }

        public string getSchema()
        {
            throw new NotImplementedException();
        }

        public void setNetworkTimeout(java.util.concurrent.Executor executor, int milliseconds)
        {
            throw new NotImplementedException();
        }

        public void setSchema(string schema)
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
