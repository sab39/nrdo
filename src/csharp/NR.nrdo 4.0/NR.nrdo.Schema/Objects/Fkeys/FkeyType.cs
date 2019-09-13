using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;

namespace NR.nrdo.Schema.Objects.Fkeys
{
    public sealed class FkeyType : SubObjectType<FkeyType, FkeyType.State, TableType, Stateless>
    {
        public override string Name { get { return "fkey"; } }

        public override IEnumerable<SubObjectState<FkeyType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from fkey in connection.GetAllForeignKeys()
                   select Create(TableType.Identifier(fkey.FromTable.QualifiedName), TableType.Identifier(fkey.ToTable.QualifiedName),
                       fkey.Name, fkey.IsCascadeDelete, from j in fkey.Joins select new FieldPair(j.FromFieldName, j.ToFieldName));
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropFkeysStep();
                yield return new AddFkeysStep();
            }
        }

        public sealed class State
        {
            private readonly ReadOnlyCollection<FieldPair> joins;
            public ReadOnlyCollection<FieldPair> Joins { get { return joins; } }

            private readonly string toTableName;
            public string ToTableName { get { return toTableName; } }

            public Identifier ToTable { get { return TableType.Identifier(toTableName); } }

            private readonly bool isCascadeDelete;
            public bool IsCascadeDelete { get { return isCascadeDelete; } }

            internal State(string toTableName, bool isCascadeDelete, IEnumerable<FieldPair> joins)
            {
                this.toTableName = toTableName;
                this.isCascadeDelete = isCascadeDelete;
                this.joins = joins.ToList().AsReadOnly();
            }

            public override string ToString()
            {
                return "fkey " + (isCascadeDelete ? "cascade " : "") + "to " + toTableName + " (" + string.Join(", ", joins) + ")";
            }
        }

        public static SubObjectState<FkeyType, State> Create(Identifier fromTable, Identifier toTable, string name, bool isCascadeDelete,
            IEnumerable<FieldPair> joins)
        {
            TableType.CheckType(fromTable);
            TableType.CheckType(toTable);
            return CreateState(fromTable, name, new State(toTable.Name, isCascadeDelete, joins));
        }

        public static bool FkeyHasChanged(SchemaChanges changes, SubObjectState<FkeyType, FkeyType.State> current)
        {
            if (current == null) return true;

            var desired = changes.Desired.Get(current);

            // The foreign key doesn't exist in the desired schema at all
            if (desired == null) return true;

            // The destination table has changed
            if (desired.State.ToTable != current.State.ToTable) return true;

            // Either the from or to table might need to be dropped entirely to reorder fields
            if (FieldType.IsFieldReorderPossiblyNeeded(changes, current.ParentIdentifier) ||
                FieldType.IsFieldReorderPossiblyNeeded(changes, current.State.ToTable)) return true;

            // It's changed from cascading to not or vice versa
            if (desired.State.IsCascadeDelete != current.State.IsCascadeDelete) return true;

            // The sequence of field name pairs don't match
            if (!Enumerable.SequenceEqual(desired.State.Joins, current.State.Joins, FieldPair.GetComparer(changes.DbDriver))) return true;

            // Any of the fields in either table are changing
            if (current.State.Joins.Any(field =>
                FieldType.FieldHasChanged(changes, current.ParentIdentifier, FieldType.Identifier(field.FromFieldName)) ||
                FieldType.FieldHasChanged(changes, current.State.ToTable, FieldType.Identifier(field.ToFieldName)))) return true;

            // The unique index on the destination table that corresponds to the destination fields is changing
            var ukey = UniqueIndexType.ChildrenFrom(changes.Current, current.State.ToTable)
                .SingleOrDefault(key => new HashSet<Identifier>(key.State.IndexState.Fields).SetEquals(from field in current.State.Joins
                                                                                                       select FieldType.Identifier(field.ToFieldName)));
            if (ukey != null && UniqueIndexType.IndexHasChanged(changes, ukey)) return true;

            return false;
        }
    }
}
