using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Util;
using System.Reflection;
using NR.nrdo.Caching;
using NR.nrdo.Connection;
using NR.nrdo.Stats;
using System.Diagnostics;

namespace NR.nrdo
{
    [Serializable]
    public abstract class DBTableObject<T> : DBObject<T>, ITableObject<T>
        where T : DBTableObject<T>
    {
        private static List<TypedMethodInvoker<T>> typedMethodInvokers = new List<TypedMethodInvoker<T>>();
        private static TypedMethodInvoker<T> getTypedMethodInvoker<TInterface>()
            where TInterface : class, ITableObject
        {
            lock (Nrdo.LockObj)
            {
                foreach (var invoker in typedMethodInvokers)
                {
                    if (invoker is ITypedMethodInvoker<T, TInterface>) return invoker;
                }
                var newInvoker = TypedMethodInvoker<T>.Create<TInterface>();
                typedMethodInvokers.Add(newInvoker);
                return newInvoker;
            }
        }

        public void InvokeTypedMethod<TInterface>(ITypedMethod<TInterface> action)
            where TInterface : class, ITableObject
        {
            getTypedMethodInvoker<TInterface>().Invoke((T)this, action);
        }

        protected abstract void setPkeyOnCmd(NrdoCommand cmd);
        protected abstract void setDataOnCmd(NrdoCommand cmd);
        protected abstract void getPkeyFromSeq(NrdoScope scope);

        protected abstract string UpdateStatement { get; }
        protected abstract string InsertStatement { get; }
        protected abstract string DeleteStatement { get; }

        protected internal static string selectStatement;

        protected static bool nrdoInitialize(Func<DataBase> getDataBase, Func<NrdoResult, T> createFromResult, string select)
        {
            nrdoInitialize(getDataBase, createFromResult);
            selectStatement = select;
            Nrdo.FullCacheFlush += DataModification.RaiseFullFlush;
            return true;
        }

        protected bool isNew;
        public bool IsNew { get { return isNew; } }

        protected virtual bool IsUnchanged { get { return false; } }

        protected virtual void copyInitialValuesOnDeserialize() { }

        public static List<T> GetAll()
        {
            if (createFromResult == null)
            {
                // We need to force the class to initialize itself. This is harder than it sounds, because
                // this is a static method, which means that here, T is just a type parameter to us, not a
                // derived class that must have been initialized. I have not yet found a way to directly
                // call the static constructor of a type parameter. We can't count on "new T()" being available,
                // we can't access any static methods on T, etc. As far as I can tell reflection is the
                // only way to do this.
                FieldInfo field = typeof(T).GetField("nrdoInitialized", BindingFlags.Static | BindingFlags.NonPublic);
                bool initialized = (bool)field.GetValue(null);
                if (!initialized) throw new ApplicationException("Nrdo Initialization of " + typeof(T).FullName + " failed (cannot call GetAll from static constructor).");
            }
            return getMulti(new AllWhere<T>());
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj)) return true;
            T t = obj as T;
            return t != null && Equals(t);
        }
        public abstract bool PkeyEquals(T t);
        public abstract int GetPkeyHashCode();
        public virtual bool Equals(T t)
        {
            return PkeyEquals(t);
        }
        public override int GetHashCode()
        {
            return GetPkeyHashCode();
        }

        public virtual T Updated()
        {
            Update();
            return (T)this;
        }
        public virtual void Update()
        {
            PreUpdateRefs();
            UpdateWithIdentityIfNecessary();
            PostUpdateRefs();
        }
        protected virtual void UpdateWithIdentityIfNecessary()
        {
            UpdateThis();
        }
        public virtual void UpdateThis()
        {
            // If we can skip it, skip it
            if (!IsNew && IsUnchanged)
            {
                Nrdo.DebugLog(() => Nrdo.DebugArgs("db-update-skipped", typeof(T).FullName, "Update", null));
                return;
            }

            // Store the ModificationCount
            var modCount = DataModification.Count;

            // Make the database change. If an exception is thrown, FullFlush then rethrow it.
            var wasNew = IsNew;
            try
            {
                // Insert or update in the database.
                var stopwatch = Stopwatch.StartNew();
                string cmdText = IsNew ? InsertStatement : UpdateStatement;
                if (cmdText != null)
                {
                    NrdoTransactedScope.MaybeBeginTransaction(DataBase);
                    using (var scope = new NrdoScope(DataBase))
                    {
                        scope.ExecuteSql(cmdText, cmd =>
                        {
                            setDataOnCmd(cmd);
                            setPkeyOnCmd(cmd);
                        });
                        if (IsNew) getPkeyFromSeq(scope);
                        isNew = false;
                    }
                    stopwatch.Stop();
                    Nrdo.DebugLog(() => Nrdo.DebugArgs(stopwatch, wasNew ? "db-insert" : "db-update", typeof(T).FullName, "Update", null));
                    NrdoStats.UpdateGlobalStats(stats => stats.WithModification(stopwatch.Elapsed));
                }
            }
            catch
            {
                DataModification.RaiseFullFlush();
                throw;
            }

            // Lock again. If the modification count is not equal to what it was, FullFlush. Otherwise raise
            // the relevant event.
            lock (Nrdo.LockObj)
            {
                if (DataModification.Count != modCount)
                {
                    DataModification.RaiseFullFlush();
                }
                else if (wasNew)
                {
                    DataModification.RaiseInsert((T)this);
                }
                else
                {
                    DataModification.RaiseUpdate((T)this);
                }
            }
        }

        public virtual void PreUpdateRefs()
        {
        }
        public virtual void PostUpdateRefs()
        {
        }

        public virtual void Delete()
        {
            // If it's not in the database in the first place,
            // deleting is a no-op.
            if (IsNew) return;

            // Store the ModificationCount
            var modCount = DataModification.Count;

            // Store a pre-deletion copy. This will be used in the modification events so that the Id can be captured by code that isn't in this table.
            var preDelete = this.FieldwiseClone();

            // Make the database change. If an exception is thrown, FullFlush then rethrow it.
            try
            {
                // Delete from the database.
                var stopwatch = Stopwatch.StartNew();
                NrdoTransactedScope.MaybeBeginTransaction(DataBase);
                using (var scope = new NrdoScope(DataBase))
                {
                    scope.ExecuteSql(DeleteStatement, setPkeyOnCmd);
                }
                isNew = true;
                stopwatch.Stop();
                Nrdo.DebugLog(() => Nrdo.DebugArgs(stopwatch, "db-delete", typeof(T).FullName, "Delete", null));
                NrdoStats.UpdateGlobalStats(stats => stats.WithModification(stopwatch.Elapsed));
            }
            catch
            {
                DataModification.RaiseFullFlush();
                throw;
            }

            // Lock again. If the modification count is not equal to what it was, FullFlush. Otherwise raise
            // the relevant event.
            lock (Nrdo.LockObj)
            {
                if (DataModification.Count != modCount)
                {
                    DataModification.RaiseFullFlush();
                }
                else
                {
                    DataModification.RaiseDelete(preDelete);
                }
            }
        }

        public static class DataModification
        {
            public static event Action<T> Insert;
            public static event Action<T> Update;
            public static event Action<T> Delete;
            public static event Action CascadeDelete;
            public static event Action FullFlush;

            private static long count = 0;
            public static long Count
            {
                get
                {
                    lock (Nrdo.LockObj) { return count; }
                }
            }

            public static event Action Any;

            public static class CascadeFrom<TOther>
                where TOther : DBTableObject<TOther>
            {
                public static event Action<TOther> Cascade;

                public static void RaiseCascadeDelete(TOther other)
                {
                    lock (Nrdo.LockObj)
                    {
                        var cascade = Cascade;
                        if (cascade != null) cascade(other);

                        var outerCascade = CascadeDelete;
                        if (outerCascade != null) outerCascade();

                        raiseAny();
                    }
                }
            }

            private static void raiseAny()
            {
                lock (Nrdo.LockObj)
                {
                    count++;
                    var any = Any;
                    if (any != null) any();
                }
            }

            // "Raise" is the wrong name here
            // Perhaps Microsoft has the right idea and "On" is the right prefix
            public static void RaiseInsert(T t)
            {
                lock (Nrdo.LockObj)
                {
                    var insert = Insert;
                    if (insert != null) insert(t);
                    raiseAny();
                }
            }

            public static void RaiseUpdate(T t)
            {
                lock (Nrdo.LockObj)
                {
                    var update = Update;
                    if (update != null) update(t);
                    raiseAny();
                }
            }

            public static void RaiseDelete(T t)
            {
                lock (Nrdo.LockObj)
                {
                    var delete = Delete;
                    if (delete != null) delete(t);
                    raiseAny();
                }
            }

            private static bool flushing;
            public static void RaiseFullFlush()
            {
                lock (Nrdo.LockObj)
                {
                    if (flushing) return;
                    try
                    {
                        flushing = true;

                        var fullFlush = FullFlush;
                        if (fullFlush != null) fullFlush();
                        raiseAny();
                    }
                    finally
                    {
                        flushing = false;
                    }
                }
            }
        }
    }
}
