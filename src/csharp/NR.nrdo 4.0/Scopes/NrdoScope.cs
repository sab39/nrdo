using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using NR.nrdo.Scopes;
using NR.nrdo.Connection;

namespace NR.nrdo
{
    public class NrdoScope : IDisposable
    {
        protected static bool IsDebugEnabled { get { return Nbool.Parse(ConfigurationManager.AppSettings["NrdoScopeDebug"]) ?? false; } }

        public static NrdoScope PeekAtCurrent(DataBase dataBase)
        {
            return getInner(dataBase);
        }

        public static void Begin()
        {
            Begin(DataBase.Default);
        }
        public static void Begin(DataBase dataBase)
        {
            var top = getTop(dataBase);
            if (top != null) throw new InvalidOperationException("Cannot use NrdoScope.Begin() when a scope already exists on the current thread. top is " + top + ", inner is " + getInner(dataBase));
            new NrdoScope(dataBase);
        }

        public static void End()
        {
            End(DataBase.Default);
        }
        public static void End(DataBase dataBase)
        {
            var top = getTop(dataBase);
            var inner = getInner(dataBase);
            if (top == null) throw new InvalidOperationException("Cannot use NrdoScope.End() when no scope is open.");
            if (top != inner) throw new InvalidOperationException("Cannot use NrdoScope.End() when nested scopes are open. top is " + top + ", inner is " + inner);
            if (top is NrdoTransactedScope) throw new InvalidOperationException("Cannot use NrdoScope.End() when the scope is transacted");
            top.Dispose();
        }

        public NrdoScope() : this(DataBase.Default) { }

        public NrdoScope(DataBase dataBase)
        {
            if (IsDebugEnabled) this.debugCreationStack = Environment.StackTrace;
            this.dataBase = dataBase;
            parent = inner;
            inner = this;
            if (top == null)
            {
                top = this;
                Nrdo.UpdateGlobalStats(stats => stats.WithScopeStart());
            }
        }

        protected NrdoScope top
        {
            get { return ScopeInfo.Current.GetDbScopeInfo(dataBase).top; }
            private set { ScopeInfo.Current.GetDbScopeInfo(dataBase).top = value; }
        }
        protected static NrdoScope getTop(DataBase dataBase)
        {
            return ScopeInfo.Current.GetDbScopeInfo(dataBase).top;
        }

        protected NrdoScope inner
        {
            get { return ScopeInfo.Current.GetDbScopeInfo(dataBase).inner; }
            private set { ScopeInfo.Current.GetDbScopeInfo(dataBase).inner = value; }
        }
        protected static NrdoScope getInner(DataBase dataBase)
        {
            return ScopeInfo.Current.GetDbScopeInfo(dataBase).inner;
        }

        protected NrdoConnection conn
        {
            get { return ScopeInfo.Current.GetDbScopeInfo(dataBase).conn; }
            private set { ScopeInfo.Current.GetDbScopeInfo(dataBase).conn = value; }
        }
        private static NrdoConnection getConn(DataBase dataBase)
        {
            return ScopeInfo.Current.GetDbScopeInfo(dataBase).conn;
        }

        protected readonly DataBase dataBase;
        protected readonly NrdoScope parent;
        protected readonly string debugCreationStack;

        public DataBase DataBase { get { return dataBase; } }
        public DbDriver DbDriver { get { return dataBase.DbDriver; } }

        public NrdoConnection GetConnection()
        {
            if (disposed) throw new InvalidCastException("Cannot get a connection from a disposed scope");
            if (conn == null)
            {
                Nrdo.UpdateGlobalStats(stats => stats.WithConnectionStart());
                conn = NrdoConnection.Create(dataBase);
            }
            return conn;
        }

        public NrdoScope PeekAtParent()
        {
            return parent;
        }

        public override string ToString()
        {
            var result = GetType().Name;
            if (debugCreationStack != null) result += " created at " + debugCreationStack + "\r\n";
            if (parent != null) result += " under " + parent;
            return result;
        }

        protected void setCommandTimeout(IDbCommand cmd, int? defaultValue)
        {
            if (ConfigurationManager.AppSettings["DatabaseCommandTimeoutSeconds"] != null)
            {
                cmd.CommandTimeout = int.Parse(ConfigurationManager.AppSettings["DatabaseCommandTimeoutSeconds"]);
            }
            else if (defaultValue != null)
            {
                cmd.CommandTimeout = (int)defaultValue;
            }
        }

        // This is unfortunately required because for some stupid reason simply creating a command on a connection with a
        // pending transaction doesn't work, you have to EXPLICITLY associate the command with the transaction. So allowing
        // the chain of scopes to participate in the creation of commands is a requirement...
        public virtual void prepareCommand(IDbCommand cmd)
        {
            if (parent == null)
            {
                setCommandTimeout(cmd, null);
            }
            else
            {
                parent.prepareCommand(cmd);
            }
        }

        public void ExecuteSql(string sql, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            GetConnection().ExecuteSql(sql, cmd =>
            {
                prepareCommand(cmd.Command);
                if (setParams != null) setParams(cmd);
            }, commandType);
        }

        public IEnumerable<T> ExecuteSql<T>(string sql, Func<NrdoResult, T> getResult, Action<NrdoCommand> setParams = null, CommandType commandType = CommandType.Text)
        {
            return GetConnection().ExecuteSql(sql, getResult, cmd =>
            {
                prepareCommand(cmd.Command);
                if (setParams != null) setParams(cmd);
            }, commandType);
        }

        protected bool disposed;

        public virtual void Dispose()
        {
            if (disposed) throw new InvalidOperationException("Cannot dispose a NrdoScope that has already been disposed: " + this);
            if (this != inner) throw new InvalidOperationException("Cannot dispose a NrdoScope that is not the current innermost scope. " + this + " inner is " + inner);
            inner = parent;
            if (this == top)
            {
                top = null;
                if (conn != null)
                {
                    conn.Dispose();
                    conn = null;
                }
            }
            disposed = true;
            GC.SuppressFinalize(this);
        }

        ~NrdoScope()
        {
            if (!disposed)
            {
                try // Try closing the connection itself first without worrying about anything in the scope hierarchy, that may be difficult to reach.
                {
                    if (conn != null)
                    {
                        conn.Dispose();
                        conn = null;
                    }
                }
                catch
                {
                    // We can't do anything useful with the exception in a finalizer anyway...
                }
                try // Then try going through the normal scope disposal process, but this may blow up in other ways like the ScopeInfo being unreachable.
                {
                    Dispose();
                }
                catch
                {
                    // We can't do anything useful with the exception in a finalizer anyway...
                }
            }
        }
    }
}
