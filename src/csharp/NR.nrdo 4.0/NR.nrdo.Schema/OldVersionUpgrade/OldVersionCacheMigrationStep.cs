using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Smelt;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Schema.Objects.Queries;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.OldVersionUpgrade
{
    internal sealed class OldVersionCacheMigrationStep : StepBase
    {
        public override string Identifier
        {
            get { return "migrating-old-nrdo-cache"; }
        }

        public override bool MustHappenAfter(StepBase other)
        {
            return true;
        }

        public override void Perform(SchemaChanges changes, IOutput output)
        {
            using (var block = output.ProgressBlock())
            {
                var desired = OldVersionCacheMigrationType.AllFrom(changes.Desired).Single();

                // Because this is guaranteed to be last in the prerequisite run, we know the storage tables are fully created and we can start using them.
                changes.SetStorageAvailable();
                var storage = changes.Storage;

                // If the migration has already run, OR the storage tables already contain Complete (implying they've already been populated by a full run
                // or migration) then we don't run it again.
                if (storage.ContainsRoot(desired.Identifier) ||
                    storage.ContainsRoot(CompletionType.Create().Identifier)) return;

                var chunks = block.GetChunks(10, 40).ToList();

                // If we can't find a cache then there's no data to upgrade with.
                var nrdoCache = desired.State.FindOldVersionNrdoCache(chunks[0].Start());
                if (nrdoCache == null) return;

                // This line serves no purpose except to cause changes to notice that this step did something and so needs to be included in the output.
                changes.Put(null);

                var total = nrdoCache.Entries.Count;

                using (var steps = chunks[1].ProgressBlock(total))
                {
                    output.Message("Migration: Processing " + total + " tables and queries...");
                    foreach (var entry in nrdoCache.Entries)
                    {
                        var cacheLine = SmeltFile.Parse(entry.Content).Lines.Single();
                        var obsoleteVersionSpec = cacheLine.GetString(0);
                        var type = cacheLine.GetString(1);
                        var name = cacheLine.GetString(2);
                        if (obsoleteVersionSpec != "") throw new ApplicationException("Unexpected value " + obsoleteVersionSpec + " found in obsolete version spec space");
                        if (name != entry.Name) throw new ApplicationException("Name in cache entry does not match file name: " + name + " vs " + entry.Name);

                        if (type == "tcache")
                        {
                            output.Verbose("Migration: Table " + name + "...");
                            // Table cache structure is either
                            //[] tcache name existing { beforestatement beforestatement ... ; };

                            // OR
                            //[] tcache name {
                            //  fieldname sqltype nullable|notnull identity? ;
                            //  ...
                            //} {
                            //  pk|uk|ix indexname { fieldname; fieldname; ... } ;
                            //  ...
                            //} {
                            //  fkeyname desttable { fromfield tofield; fromfield tofield; ... } cascade? ;
                            //  ...
                            //} {
                            //  sequencename ;
                            //  triggername ;
                            //  beforestatement beforestatement ... ;
                            //};
                            if (cacheLine.Words[3] is SmeltString)
                            {
                                if (cacheLine.GetString(3) != "existing") throw new ApplicationException("Unexpected word '" + cacheLine.GetString(3) + "' in " + name);
                                var beforeStatements = cacheLine.GetBlock(4).Lines.Single();
                                if (beforeStatements.Words.Any()) throw new ApplicationException("Table " + name + " was 'existing' but has before statements, which is no longer supported");
                                // In the old world "existing" tables could have before statements, but we can't do that any more
                            }
                            else
                            {

                                var table = TableType.Identifier(name);
                                storage.PutRoot(table);

                                foreach (var fieldLine in cacheLine.GetBlock(3).Lines)
                                {
                                    storage.PutSub(table, FieldType.Identifier(fieldLine.GetString(0)));
                                    // We don't care about type, nullability or identity any more because we can get all that from the database schema directly
                                }

                                foreach (var ixLine in cacheLine.GetBlock(4).Lines)
                                {
                                    var ixTypeStr = ixLine.GetString(0);
                                    ObjectType ixType;
                                    switch (ixTypeStr)
                                    {
                                        case "ix": ixType = NonUniqueIndexType.Instance; break;
                                        case "pk":
                                        case "uk": ixType = UniqueIndexType.Instance; break;
                                        default: throw new ApplicationException("Unexpected index type " + ixTypeStr + " in " + name);
                                    }
                                    storage.PutSub(table, ixType.Identifier(ixLine.GetString(1)));
                                }

                                foreach (var fkLine in cacheLine.GetBlock(5).Lines)
                                {
                                    storage.PutSub(table, FkeyType.Identifier(fkLine.GetString(0)));
                                }

                                var others = cacheLine.GetBlock(6);

                                var seqInfo = others.Lines[0];
                                if (seqInfo.Words.Count > 1 && changes.SchemaDriver.IsSequenceUsed)
                                {
                                    storage.PutSub(table, SequenceType.Identifier("dbo." + seqInfo.GetString(1)));
                                }

                                if (seqInfo.Words.Count > 2 && changes.SchemaDriver.IsTriggerUsedForSequence)
                                {
                                    storage.PutSub(table, TriggerType.Identifier("dbo." + seqInfo.GetString(2)));
                                }

                                if (others.Lines.Count > 1)
                                {
                                    foreach (var beforeStatement in others.Lines[1].Words.Cast<SmeltString>())
                                    {
                                        storage.PutSub(table, BeforeStatementType.Identifier(beforeStatement.Text));
                                    }
                                }
                            }
                        }
                        else
                        {
                            output.Verbose("Migration: Query " + name);
                            // Query cache file structure is
                            //  [] spcache name [] { beforestatement beforestatement ... ; };
                            // OR
                            //  [] spcache|sfcache|spcache-preupgrade name [sql] { beforestatement beforestatement ... ; }

                            var isPreUpgradeHook = false;
                            switch (type)
                            {
                                case "spcache":
                                case "sfcache": break;
                                case "spcache-preupgrade": isPreUpgradeHook = true; break;
                                default: throw new ApplicationException("Unknown type of cache entry '" + type + "' in " + name);
                            }

                            var query = QueryType.Identifier(name);
                            storage.PutRoot(query);

                            if (isPreUpgradeHook) storage.PutSub(query, PreUpgradeHookType.Create(query).Identifier);

                            foreach (var beforeStatement in cacheLine.GetBlock(4).Lines.Single().Words.Cast<SmeltString>())
                            {
                                storage.PutSub(query, BeforeStatementType.Identifier(beforeStatement.Text));
                            }
                        }
                        steps.Step++;
                    }
                    if (nrdoCache.IsComplete) storage.PutRoot(CompletionType.Create().Identifier);
                    storage.PutRoot(desired.Identifier);
                    output.Message("Migration: Complete.");
                }
            }
        }
    }
}
