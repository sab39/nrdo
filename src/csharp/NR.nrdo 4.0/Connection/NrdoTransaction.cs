using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public sealed class NrdoTransaction : IDisposable
    {
        private readonly IDbTransaction transaction;

        // IsActive remains true until Dispose is called. New transactions cannot be created until this happens.
        private bool isActive = true;
        public bool IsActive { get { return isActive; } }

        // isUsable remains true until Commit() or RollBack() is called. After this, the connection cannot be used until
        // Dispose is called. If Dispose is reached when IsUsable is still true, that's an error.
        private bool isUsable = true;

        // isRollBackable is the same as isUsable except in the case where Commit() fails. In that case the connection isn't
        // usable in general, but calling RollBack is still allowed.
        private bool isRollbackable = true;

        internal NrdoTransaction(IDbConnection connection)
        {
            this.transaction = connection.BeginTransaction();
        }

        internal void ApplyToCommand(IDbCommand cmd)
        {
            checkUsable();
            cmd.Transaction = transaction;
        }

        public void Commit()
        {
            checkUsable();
            this.isUsable = false;
            transaction.Commit();
            this.isRollbackable = false;
            transaction.Dispose();
        }

        public void RollBack()
        {
            if (!this.isRollbackable) throw new InvalidOperationException("Transaction cannot be used after it's been committed or rolled back.");
            try
            {
                transaction.Rollback();
            }
            finally
            {
                this.isUsable = false;
                this.isRollbackable = false;
                transaction.Dispose();
            }
        }

        private void checkUsable()
        {
            if (!this.isUsable) throw new InvalidOperationException("Transaction cannot be used after it's been committed or rolled back.");
        }

        public void Dispose()
        {
            transaction.Dispose();
            this.isActive = false;
            if (this.isUsable || this.isRollbackable)
            {
                this.isUsable = false;
                this.isRollbackable = false;
                throw new InvalidOperationException("Transaction should be committed or rolled back before disposing");
            }
        }
    }
}
