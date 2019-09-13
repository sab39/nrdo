using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Schema.Providers;
using NR.nrdo.Schema.State;
using System.Collections.Immutable;
using NR.nrdo.Util.OutputUtil;
using NR.nrdo.Util;

namespace NR.nrdo.Schema.Tool
{
    public static class SchemaTool
    {
        private static readonly ImmutableList<ISchemaProvider> essentialProviders =
            ImmutableList.Create<ISchemaProvider>(new PrerequisiteSchemaProvider(), new EssentialSchemaProvider());

        public static void UpdateSchema(SchemaDriver schemaDriver, string connectionString, IOutput output, params ISchemaProvider[] providers)
        {
            UpdateSchema(schemaDriver, connectionString, output, SchemaChangeOptions.Default, providers.AsEnumerable());
        }
        public static void UpdateSchema(SchemaDriver schemaDriver, string connectionString, IOutput output, IEnumerable<ISchemaProvider> providers)
        {
            UpdateSchema(schemaDriver, connectionString, output, SchemaChangeOptions.Default, providers);
        }
        public static void UpdateSchema(SchemaDriver schemaDriver, string connectionString, IOutput output, SchemaChangeOptions options, params ISchemaProvider[] providers)
        {
            UpdateSchema(schemaDriver, connectionString, output, options, providers.AsEnumerable());
        }
        public static void UpdateSchema(SchemaDriver schemaDriver, string connectionString, IOutput output, SchemaChangeOptions options, IEnumerable<ISchemaProvider> providers)
        {
            var allProviders = essentialProviders.AddRange(providers);
            var preProviders = allProviders.OfType<IPrerequisiteSchemaProvider>();
            var normalProviders = from provider in allProviders
                                  let pre = provider as IPrerequisiteSchemaProvider
                                  where pre == null || pre.IncludeInNormalRun
                                  select provider;

            using (var progress = output.ProgressBlock())
            {
                var currentState = DatabaseState.Empty;
                var objectTypes = ImmutableHashSet<ObjectType>.Empty;
                StateStorage storage = null;

                var chunks = progress.GetChunks(1, 19, providers == null ? 0 : 80).ToList();
                output.Message("Connecting to database...");
                using (var connection = SchemaConnection.Create(schemaDriver, connectionString))
                {
                    connection.AcquireSchemaUpdateLock(output, options.SchemaUpdateLockTimeout);
                    output.Verbose("Connected to database.");
                    chunks[0].ProgressComplete();
                    runSteps(connection, chunks[1], options, "Preparation", preProviders, ref currentState, ref objectTypes, ref storage);
                    runSteps(connection, chunks[2], options, "Processing", normalProviders, ref currentState, ref objectTypes, ref storage);
                }
            }
        }

        private static string counted(int count, string singular, string plural = null, string zero = null)
        {
            if (count == 0)
            {
                return zero ??
                    "No " + (plural ?? (singular + "s"));
            }
            else if (count == 1)
            {
                return "1 " + singular;
            }
            else
            {
                return count + " " + (plural ?? (singular + "s"));
            }
        }

        private static void runSteps(SchemaConnection connection, IOutput output, SchemaChangeOptions options, string phaseName, IEnumerable<ISchemaProvider> providers,
            ref DatabaseState currentState, ref ImmutableHashSet<ObjectType> objectTypes, ref StateStorage storage)
        {
            using (var progress = output.ProgressBlock())
            {
                output.Message(phaseName + ": Starting...");

                // Get object types, get steps, get desired state, get current state, everything else
                var chunks = progress.GetChunks(1, 1, 5, 5, 1, 50).ToList();

                var oldTypes = objectTypes;
                using (chunks[0].Start())
                {
                    objectTypes = (from provider in providers
                                   from objectType in provider.GetObjectTypes(connection.SchemaDriver)
                                   select objectType).ToImmutableHashSet();
                    output.Verbose(phaseName + ": Object types: " + string.Join(", ", objectTypes));
                }

                // Get all the steps and organize them into order
                IImmutableList<StepBase> steps;
                using (chunks[1].Start())
                {
                    steps = sortSteps(from objectType in objectTypes
                                      from step in objectType.Steps
                                      select step);
                    output.Verbose(phaseName + ": Steps: " + string.Join(", ", steps));
                }

                // Get the desired state
                DatabaseState desiredState;
                using (chunks[2].Start())
                {
                    output.Verbose(phaseName + ": Determining desired database state...");
                    desiredState = DatabaseState.Create(from provider in providers
                                                        from state in provider.GetDesiredState(connection, chunks[2])
                                                        select state);

                    foreach (var overrideProvider in providers.OfType<ISchemaOverrideProvider>())
                    {
                        desiredState = overrideProvider.ApplyOverrides(connection, desiredState);
                    }

                    output.Message(phaseName + ": " + counted(desiredState.Count, "object") + " desired.");
                }

                ObjectTypeHelper helper;
                using (chunks[3].Start())
                {
                    // Get the current state
                    output.Verbose(phaseName + ": Determining current database state...");
                    helper = new ObjectTypeHelper(objectTypes, storage != null, connection.DbDriver);
                    currentState = currentState.With(from objectType in objectTypes
                                                     where !oldTypes.Contains(objectType)
                                                     from state in objectType.GetExistingObjects(connection, helper)
                                                     select state);
                    output.Verbose(phaseName + ": " + counted(currentState.Count, "object") + " existing.");
                }

                // Filter to only things that are relevant here
                using (chunks[4].Start())
                {
                    output.Verbose(phaseName + ": Filtering...");
                    if (storage != null)
                    {
                        var priorKnownRoots = storage.GetKnownRoots(helper);
                        var priorKnownSubs = storage.GetKnownSubs(helper);

                        // Anything that exists in *both* current and desired is treated as known and assumed to be in-scope for consideration
                        var knownRoots = priorKnownRoots.Union(from root in currentState.RootObjects
                                                               where desiredState.Contains(root)
                                                               select root.Identifier);
                        foreach (var extra in knownRoots.Except(priorKnownRoots))
                        {
                            storage.PutRoot(extra);
                        }
                        // FIXME: remove from storage anything that's of a type that's under consideration but is neither current nor desired

                        var knownSubs = priorKnownSubs.Union(from sub in currentState.AllChildren
                                                             where desiredState.Contains(sub)
                                                             select Tuple.Create(sub.ParentIdentifier, sub.Identifier));
                        foreach (var extra in knownSubs.Except(priorKnownSubs))
                        {
                            storage.PutSub(extra.Item1, extra.Item2);
                        }
                        // FIXME: remove from storage anything that's of a type that's under consideration but is neither current nor desired

                        var _storage = storage;
                        var initialState = currentState;
                        currentState = currentState.WithFilters((root) => desiredState.ContainsRoot(root) ||
                                                                          Objects.Tables.TableRenameType.ContainsRenameTo(desiredState, root) ||
                                                                          Objects.Tables.PendingReorderTableType.IsTablePendingReorder(initialState, root) ||
                                                                          (options.PreserveUnknownObjects ? !initialState.ContainsRoot(root) : _storage.ContainsRoot(root)),
                                                                null);
                    }
                    else
                    {
                        // FIXME this doesn't allow for any flexibility in whether the nrdo_ tables live in dbo
                        currentState = currentState.WithFilters(root => root.Name.StartsWith("dbo.nrdo_", StringComparison.OrdinalIgnoreCase),
                                                                null);
                    }
                    output.Message(phaseName + ": " + counted(currentState.Count, "applicable existing object") + ".");
                }

                var isFullRun = storage != null;
                if (isFullRun)
                {
                    // Sanity check for any before statements that will be skipped due to the associated step not existing
                    var stepSet = new HashSet<string>(from step in steps select step.Identifier, Nstring.DBEquivalentComparer);
                    var current = currentState; // Can't use ref parameter directly inside linq query
                    var unknownBefores = from stmt in BeforeStatementType.AllFrom(desiredState)
                                         where !stepSet.Contains(stmt.State.Step) && !current.Contains(stmt)
                                         select stmt;
                    foreach (var stmt in unknownBefores)
                    {
                        output.Warning(stmt.Name + " on " + stmt.ParentIdentifier + " will not be executed because there is no such step as " + stmt.State.Step);
                    }
                }

                var changes = SchemaChanges.Create(currentState, desiredState, connection, chunks[5], options, storage);

                changes.PerformSteps(steps, phaseName, isFullRun);

                currentState = changes.Current.WithoutFilters();
                storage = changes.Storage;

                if (changes.HasFailed) throw new ApplicationException("Schema update failed");

                output.Message(phaseName + ": Completed.");
                output.Message(phaseName + ": " + counted(changes.StatementCount, "statement") + " executed.");
                output.Message(phaseName + ": " + counted(currentState.Count, "object") + " existing.");
            }
        }

        private static ImmutableList<StepBase> sortSteps(IEnumerable<StepBase> steps)
        {
            return steps.DependencySort((a, b) => a.MustHappenBefore(b) || b.MustHappenAfter(a)).ToImmutableList();
        }
    }
}
