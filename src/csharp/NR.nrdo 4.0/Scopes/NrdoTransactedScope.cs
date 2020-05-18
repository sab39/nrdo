using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Scopes;
using NR.nrdo.Stats;

namespace NR.nrdo
{
    public sealed class NrdoTransactedScope : NrdoScope
    {
        private NrdoTransactedScope topTransacted
        {
            get { return ScopeInfo.Current.GetDbScopeInfo(dataBase).topTransacted; }
            set { ScopeInfo.Current.GetDbScopeInfo(dataBase).topTransacted = value; }
        }
        private static NrdoTransactedScope getTopTransacted(DataBase dataBase)
        {
            return ScopeInfo.Current.GetDbScopeInfo(dataBase).topTransacted;
        }

        private IDbTransaction tx
        {
            get { return ScopeInfo.Current.GetDbScopeInfo(dataBase).tx; }
            set { ScopeInfo.Current.GetDbScopeInfo(dataBase).tx = value; }
        }
        private static IDbTransaction getTx(DataBase dataBase)
        {
            return ScopeInfo.Current.GetDbScopeInfo(dataBase).tx;
        }

        new public static void Begin()
        {
            Begin(DataBase.Default);
        }
        new public static void Begin(DataBase dataBase)
        {
            var top = getTop(dataBase);
            if (top != null) throw new InvalidOperationException("Cannot use NrdoTransactedScope.Begin() when a scope already exists on the current thread. top is " + top + ", inner is " + getInner(dataBase) + ", topTransacted is " + getTopTransacted(dataBase));
            new NrdoTransactedScope(dataBase);
        }

        new public static void End()
        {
            End(DataBase.Default);
        }
        new public static void End(DataBase dataBase)
        {
            var top = getTop(dataBase);
            if (top == null) throw new InvalidOperationException("Cannot use NrdoTransactedScope.End() when no scope is open.");
            if (top != getInner(dataBase)) throw new InvalidOperationException("Cannot use NrdoTransactedScope.End() when nested scopes are open. top is " + top + ", inner is " + getInner(dataBase) + ", topTransacted is " + getTopTransacted(dataBase));
            if (!(top is NrdoTransactedScope)) throw new InvalidOperationException("Cannot use NrdoTransactedScope.End() when the scope is not transacted. top is " + top + ", inner is " + getInner(dataBase) + ", topTransacted is " + getTopTransacted(dataBase));
            top.Dispose();
        }

        public NrdoTransactedScope() : this(DataBase.Default) { }

        public NrdoTransactedScope(DataBase dataBase)
            : base(dataBase)
        {
            if (topTransacted == null) topTransacted = this;
        }

        public override void prepareCommand(IDbCommand cmd)
        {
            cmd.Transaction = tx;
            setCommandTimeout(cmd, 60);
        }

        /// <summary>
        /// Commits the current transaction, if any. If no transacted scope is
        /// present, this is a no-op because everything is already committed.
        /// </summary>
        public static void Commit()
        {
            Commit(DataBase.Default);
        }
        public static void Commit(DataBase dataBase)
        {
            if (getTopTransacted(dataBase) != null)
            {
                var tx = getTx(dataBase);
                if (tx != null)
                {
                    tx.Commit();
                    ScopeInfo.Current.GetDbScopeInfo(dataBase).tx = null;
                }
            }
        }
        /// <summary>
        /// Rolls back the current transaction, if any. If no transacted scope is
        /// present, an exception is thrown.
        /// </summary>
        public static void Rollback()
        {
            Rollback(DataBase.Default);
        }
        public static void Rollback(DataBase dataBase)
        {
            if (getTopTransacted(dataBase) == null) throw new InvalidOperationException("Cannot roll back when no transacted scope is present");
            var tx = getTx(dataBase);
            if (tx != null)
            {
                Nrdo.FlushCache(); // The cache contains stuff that might be blown away by the rollback, so remove it.
                tx.Rollback();
                ScopeInfo.Current.GetDbScopeInfo(dataBase).tx = null;
            }
        }
        /// <summary>
        /// Begins a transaction on the current transacted scope, if any is present.
        /// If no transacted scope is present, this is a no-op.
        /// </summary>
        public static void MaybeBeginTransaction()
        {
            MaybeBeginTransaction(DataBase.Default);
        }
        public static void MaybeBeginTransaction(DataBase dataBase)
        {
            if (getTopTransacted(dataBase) != null) BeginTransaction();
        }

        /// <summary>
        /// Begins a transaction on the current transacted scope, if any is present.
        /// If no transacted scope is present, an exception is thrown.
        /// </summary>
        public static void BeginTransaction()
        {
            BeginTransaction(DataBase.Default);
        }
        public static void BeginTransaction(DataBase dataBase)
        {
            var topTransacted = getTopTransacted(dataBase);
            if (topTransacted == null) throw new InvalidOperationException("Cannot begin a transaction when no transacted scope is present.");
            if (ScopeInfo.Current.GetDbScopeInfo(dataBase).tx == null)
            {
                NrdoStats.UpdateGlobalStats(stats => stats.WithTransactionStart());
                ScopeInfo.Current.GetDbScopeInfo(dataBase).tx = topTransacted.GetConnection().Connection.BeginTransaction();
            }
        }

        public override void Dispose()
        {
            if (disposed) throw new InvalidOperationException("Cannot dispose a NrdoScope that has already been disposed");
            if (this != inner) throw new InvalidOperationException("Cannot dispose a NrdoScope that is not the current innermost scope.");
            if (this == topTransacted)
            {
                Commit();
                topTransacted = null;
            }
            base.Dispose();
        }
    }
}
