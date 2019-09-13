using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NR.nrdo.Connection;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Schema.State;
using System.Collections.Immutable;
using System.Data.Common;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Tool
{
    public sealed class SchemaChanges
    {
        private DatabaseState current;
        public DatabaseState Current { get { return current; } }

        private readonly DatabaseState desired;
        public DatabaseState Desired { get { return desired; } }

        private readonly bool isUpgrade;
        public bool IsUpgrade { get { return isUpgrade; } }

        public SchemaDriver SchemaDriver { get { return connection.SchemaDriver; } }
        public DbDriver DbDriver { get { return SchemaDriver.DbDriver; } }

        private readonly SchemaConnection connection;
        public SchemaConnection Connection { get { return connection; } }

        private readonly IOutput output;
        public IOutput Output { get { return output; } }

        private readonly SchemaChangeOptions options;
        public SchemaChangeOptions Options { get { return options; } }

        private bool isStorageAvailable;
        private StateStorage storage;

        public StateStorage Storage
        {
            get
            {
                if (storage == null && isStorageAvailable) storage = new StateStorage(Connection);
                return storage;
            }
        }

        internal void SetStorageAvailable()
        {
            isStorageAvailable = true;
        }

        private SchemaChanges(DatabaseState current, DatabaseState desired, SchemaConnection connection, IOutput output, SchemaChangeOptions options, StateStorage storage)
        {
            this.current = current;
            this.desired = desired;
            this.connection = connection;
            this.output = output;
            this.options = options;
            this.storage = storage;

            this.isUpgrade = current.Contains(CompletionType.Create());
        }

        public static SchemaChanges Create(DatabaseState current, DatabaseState desired, SchemaConnection connection, IOutput output, SchemaChangeOptions options, StateStorage storage)
        {
            return new SchemaChanges(current, desired, connection, output, options, storage);
        }

        public int StatementCount { get; private set; }

        private Action stepAction;

        private bool hasFailed;
        public bool HasFailed { get { return hasFailed; } }

        public void Fail(string message)
        {
            output.SetStatus(OutputMode.Error);
            if (message != null) output.Error(message);
            hasFailed = true;
        }

        private void doStepAction()
        {
            if (stepAction != null)
            {
                stepAction();
                stepAction = null;
            }
        }

        private void executeChange(string sql, Func<DatabaseState, DatabaseState> updateState, Action<StateStorage> updateStorage, ErrorResponse errorResponse, bool useTransaction)
        {
            doStepAction();

            bool storageRefreshNeeded = false;
            try
            {
                while (true)
                {
                    using (var transaction = useTransaction ? connection.StartTransaction() : null)
                    {
                        try
                        {
                            if (sql != null)
                            {
                                StatementCount++;
                                Output.Verbose(sql);
                                try
                                {
                                    connection.ExecuteSql(sql);
                                }
                                catch (DbException ex)
                                {
                                    if (useTransaction) transaction.RollBack();

                                    output.ReportVerbose("SQL statement failed:\r\n" + sql + "\r\n" + ex);

                                    if (!HasFailed &&
                                        errorResponse.HasFlag(ErrorResponse.FlagBit_PromptToRetry) &&
                                        output.CanPrompt)
                                    {
                                        if (output.Prompt(OutputMode.Warning,
                                                          "SQL Statement failed:\r\n" + sql.Substring(0, Math.Min(sql.Length, 200)) + "...\r\n" + ex.Message,
                                                          "Try again?")) continue;
                                    }

                                    if (errorResponse.HasFlag(ErrorResponse.FlagBit_Warn))
                                    {
                                        output.Report(errorResponse.HasFlag(ErrorResponse.FlagBit_SetFailureState) ? OutputMode.Error : OutputMode.Warning,
                                                      "SQL Statement failed:\r\n" + sql.Substring(0, Math.Min(sql.Length, 200)) + "...\r\n" + ex.Message);
                                    }

                                    if (errorResponse.HasFlag(ErrorResponse.FlagBit_SetFailureState))
                                    {
                                        Fail(null);
                                    }

                                    if (errorResponse.HasFlag(ErrorResponse.FlagBit_Throw))
                                    {
                                        throw;
                                    }
                                    else
                                    {
                                        return;
                                    }
                                }
                            }

                            var newCurrent = updateState(current);
                            if (Storage != null)
                            {
                                storageRefreshNeeded = true;
                                updateStorage(Storage);
                            }
                            if (useTransaction) transaction.Commit();

                            storageRefreshNeeded = false;
                            current = newCurrent;
                            return;
                        }
                        catch
                        {
                            if (useTransaction) transaction.RollBack();
                            throw;
                        }
                    }
                }
            }
            finally
            {
                if (storageRefreshNeeded && Storage != null) Storage.Refresh();
            }
        }

        public void Put(string sql, params ObjectState[] objects)
        {
            Put(sql, ErrorResponse.Fail, objects.AsEnumerable());
        }
        public void Put(string sql, IEnumerable<ObjectState> objects)
        {
            Put(sql, ErrorResponse.Fail, objects);
        }
        public void Put(string sql, ErrorResponse errorResponse, params ObjectState[] objects)
        {
            Put(sql, errorResponse, objects.AsEnumerable());
        }
        public void Put(string sql, ErrorResponse errorResponse, IEnumerable<ObjectState> objects)
        {
            executeChange(sql, state => state.With(objects), storage => storage.PutAll(objects), errorResponse, true);
        }

        public void PutWithoutTransaction(string sql, params ObjectState[] objects)
        {
            PutWithoutTransaction(sql, ErrorResponse.Fail, objects.AsEnumerable());
        }
        public void PutWithoutTransaction(string sql, IEnumerable<ObjectState> objects)
        {
            PutWithoutTransaction(sql, ErrorResponse.Fail, objects);
        }
        public void PutWithoutTransaction(string sql, ErrorResponse errorResponse, params ObjectState[] objects)
        {
            PutWithoutTransaction(sql, errorResponse, objects.AsEnumerable());
        }
        public void PutWithoutTransaction(string sql, ErrorResponse errorResponse, IEnumerable<ObjectState> objects)
        {
            executeChange(sql, state => state.With(objects), storage => storage.PutAll(objects), errorResponse, false);
        }

        public void Remove(string sql, params ObjectState[] objects)
        {
            Remove(sql, ErrorResponse.Fail, objects.AsEnumerable());
        }
        public void Remove(string sql, IEnumerable<ObjectState> objects)
        {
            Remove(sql, ErrorResponse.Fail, objects);
        }
        public void Remove(string sql, ErrorResponse errorResponse, params ObjectState[] objects)
        {
            Remove(sql, errorResponse, objects.AsEnumerable());
        }
        public void Remove(string sql, ErrorResponse errorResponse, IEnumerable<ObjectState> objects)
        {
            executeChange(sql, state => state.Without(objects), storage => storage.DeleteAll(objects), errorResponse, true);
        }

        public void RemoveWithoutTransaction(string sql, params ObjectState[] objects)
        {
            RemoveWithoutTransaction(sql, ErrorResponse.Fail, objects.AsEnumerable());
        }
        public void RemoveWithoutTransaction(string sql, IEnumerable<ObjectState> objects)
        {
            RemoveWithoutTransaction(sql, ErrorResponse.Fail, objects);
        }
        public void RemoveWithoutTransaction(string sql, ErrorResponse errorResponse, params ObjectState[] objects)
        {
            RemoveWithoutTransaction(sql, errorResponse, objects.AsEnumerable());
        }
        public void RemoveWithoutTransaction(string sql, ErrorResponse errorResponse, IEnumerable<ObjectState> objects)
        {
            executeChange(sql, state => state.Without(objects), storage => storage.DeleteAll(objects), errorResponse, false);
        }

        public void Rename(string sql, Identifier from, Identifier to)
        {
            Rename(sql, ErrorResponse.Fail, from, to);
        }
        public void Rename(string sql, ErrorResponse errorResponse, Identifier from, Identifier to)
        {
            if (!object.Equals(from.ObjectType, to.ObjectType)) throw new ArgumentException("Cannot rename between different object types " + from.ObjectType + " and " + to.ObjectType);

            executeChange(sql, state => state.WithRename(from, to), storage => storage.Rename(from, to), errorResponse, true);
        }

        public bool AllowDropWithPossibleDataLoss(ObjectState dropItem, DropBehavior dropBehavior)
        {
            // If we're skipping drops then no drops are allowed.
            if (dropBehavior == DropBehavior.SkipDrop) return false;

            // If we're allowing data loss without prompting then it's permitted.
            if (!Options.PromptForPossibleDataLoss) return true;

            if (Output.CanPrompt)
            {
                doStepAction();

                // If the user allows it then it's allowed.
                if (Output.Prompt(OutputMode.Warning,
                                  "About to drop " + dropItem + ". All data in this " + dropItem.ObjectType + " will be lost!",
                                  "Are you sure?")) return true;
            }

            // We're not going to allow the drop; 
            if (dropBehavior == DropBehavior.Drop)
            {
                Fail("Dropping " + dropItem + " may cause data loss.");
            }
            else
            {
                output.Verbose("Skipping drop of " + dropItem);
            }

            return false;
        }

        internal void PerformSteps(IEnumerable<StepBase> steps, string phaseName, bool isFullRun)
        {
            using (var progress = output.ProgressBlock(steps.Count() * (isFullRun ? 1 : 2)))
            {
                foreach (var step in steps)
                {
                    output.Verbose(phaseName + ": Beginning " + step.Identifier + " step...");

                    if (isFullRun)
                    {
                        this.stepAction = () => output.Message(phaseName + ": Before " + step.Identifier + "...");
                        performBeforeStatements(step.Identifier, progress.BlockForStep());
                    }

                    if (HasFailed) return;

                    this.stepAction = () => output.Message(phaseName + ": " + step.Identifier + "...");
                    step.Perform(this, progress.BlockForStep());

                    if (HasFailed) return;

                    output.Verbose(phaseName + ": " + step.Identifier + " step complete.");
                    progress.Step++;
                }
            }
            SetStorageAvailable();
        }

        private void performBeforeStatements(string stepIdentifier, IOutput output)
        {
            if (HasFailed) return;

            // The guarantees made about the ordering of before statements are:
            // - Before statements on the stame step for the same parent object will execute together
            // - Before statements on the same step for the same parent object will execute in the order they were defined on that object
            // In an ideal world there would be no guarantees about the ordering of before statements between different top level objects, but
            // old versions of nrdo happened to process top-level objects in a specific order (tables in alphabetical order, followed by queries in
            // alphabetical order) and we have some code that relies on that ordering, so we need to continue to follow it.

            var beforeStatements = (from statement in BeforeStatementType.AllFrom(desired)
                                   where statement.State.Step == stepIdentifier && !current.Contains(statement)
                                   orderby statement.State.ParentSortKey, statement.State.SortPositionWithinParent
                                   select statement).ToImmutableList();

            using (var progress = output.ProgressBlock(beforeStatements.Count))
            {
                foreach (var statement in beforeStatements)
                {
                    var shouldProcess = isUpgrade ? statement.State.IsRunOnUpgrade : statement.State.IsRunOnInitialCreate;
                    if (shouldProcess)
                    {
                        doStepAction();
                        progress.Verbose(statement.ParentIdentifier + " before " + stepIdentifier + " " + statement.Name + ":");
                        Put(statement.State.Sql, statement);
                        if (HasFailed) return;
                    }
                    else
                    {
                        Put(null, statement);
                    }
                    progress.Step++;
                }
            }
        }
    }
}
