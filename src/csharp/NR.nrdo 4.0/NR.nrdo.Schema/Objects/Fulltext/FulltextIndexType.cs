using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.State;
using System.Collections.ObjectModel;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Tool;
using System.Collections.Immutable;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;

namespace NR.nrdo.Schema.Objects.Fulltext
{
    public sealed class FulltextIndexType : SubObjectType<FulltextIndexType, FulltextIndexType.State, FulltextCatalogType, Stateless>
    {
        public override string Name { get { return "fulltext-index"; } }

        public override IEnumerable<SubObjectState<FulltextIndexType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from fulltextIndex in connection.GetAllFulltextIndexes()
                   select Create(TableType.Identifier(fulltextIndex.Table.QualifiedName), FulltextCatalogType.Identifier(fulltextIndex.Catalog),
                       fulltextIndex.KeyName, fulltextIndex.ColumnNames);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropFulltextIndexesStep();
                yield return new AddFulltextIndexesStep();
            }
        }

        public sealed class State
        {
            private readonly ReadOnlyCollection<string> columns;
            public ReadOnlyCollection<string> Columns { get { return columns; } }

            private readonly string keyName;
            public string KeyName { get { return keyName; } }

            internal State(string keyName, IEnumerable<string> columns)
            {
                this.keyName = keyName;
                this.columns = columns.ToList().AsReadOnly();
            }

            public override string ToString()
            {
                return "fulltext-index on " + keyName + " (" + string.Join(", ", columns) + ")";
            }
        }

        public static SubObjectState<FulltextIndexType, State> Create(Identifier table, Identifier catalog, string keyName, IEnumerable<string> columns)
        {
            TableType.CheckType(table);
            FulltextCatalogType.CheckType(catalog);
            return CreateState(catalog, table.Name, new State(keyName, columns));
        }

        internal static bool FulltextIndexHasChanged(SchemaChanges changes, SubObjectState<FulltextIndexType, State> current)
        {
            if (current == null) return true;

            var desired = changes.Desired.Get(current);

            // The fulltext index doesn't exist in the desired schema at all
            if (desired == null) return true;

            // The key index name has changed
            if (desired.State.KeyName != current.State.KeyName) return true;

            // The set of affected columns has changed (in some way other than just the ordering)
            if (!ImmutableHashSet.CreateRange(changes.DbDriver.DbStringComparer, desired.State.Columns).SetEquals(current.State.Columns)) return true;

            var table = TableType.Identifier(desired.Name);

            // Any of the columns in the fulltext index have changed
            if (desired.State.Columns.Any(col => FieldType.FieldHasChanged(changes, table, FieldType.Identifier(col)))) return true;

            // The key index is to be dropped
            // This includes the case where the table itself may be dropped for reordering
            if (UniqueIndexType.IndexHasChanged(changes, UniqueIndexType.GetFrom(changes.Current, table, UniqueIndexType.Identifier(current.State.KeyName)))) return true;

            return false;
        }
    }
}
