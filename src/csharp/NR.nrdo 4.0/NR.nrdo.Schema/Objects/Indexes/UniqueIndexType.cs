using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;

namespace NR.nrdo.Schema.Objects.Indexes
{
    public sealed class UniqueIndexType : SubObjectType<UniqueIndexType, UniqueIndexType.State, TableType, Stateless>
    {
        public override string Name { get { return "ukey"; } }

        public override IEnumerable<SubObjectState<UniqueIndexType, UniqueIndexType.State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from index in connection.GetAllUniqueIndexes()
                   join custom in connection.GetAllIndexCustomState()
                        on new { table = TableType.Identifier(index.Table.QualifiedName), index = UniqueIndexType.Identifier(index.Name) }
                        equals new { table = TableType.Identifier(custom.Table.QualifiedName), index = UniqueIndexType.Identifier(custom.IndexName) }
                        into indexCustom
                   select CreateState(TableType.Identifier(index.Table.QualifiedName), index.Name,
                                 new State(index.IsPrimaryKey, from field in index.FieldNames select FieldType.Identifier(field),
                                           indexCustom.Any() ? indexCustom.Single().CustomState :
                                               index.IsPrimaryKey ? connection.SchemaDriver.DefaultPrimaryKeyCustomState
                                                                  : connection.SchemaDriver.DefaultUniqueConstraintCustomState));
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropIndexesStep();
                yield return new AddIndexesStep();
            }
        }

        public sealed class State
        {
            private readonly bool isPrimaryKey;
            public bool IsPrimaryKey { get { return isPrimaryKey; } }

            private readonly IndexState indexState;
            public IndexState IndexState { get { return indexState; } }

            IEnumerable<string> FieldNames { get { return indexState.FieldNames; } }

            IEnumerable<Identifier> Fields { get { return indexState.Fields; } }

            public State(bool isPrimaryKey, IEnumerable<Identifier> fields, IndexCustomState customState)
            {
                this.isPrimaryKey = isPrimaryKey;
                this.indexState = new IndexState(fields, customState);
            }

            public override bool Equals(object obj)
            {
                var other = obj as State;
                return other != null && isPrimaryKey == other.isPrimaryKey && object.Equals(indexState, other.indexState);
            }

            public override int GetHashCode()
            {
                // If we never use these as hash keys we don't need GetHashCode(), but not overriding it at all when Equals() is overridden breaks the
                // .NET-wide object.Equals()/GetHashCode() contract. Implementing it to throw solves that problem.
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return (isPrimaryKey ? "primary key" : "unique") + indexState;
            }
        }

        public static SubObjectState<UniqueIndexType, State> Create(Identifier table, string name, bool isPrimaryKey, IEnumerable<Identifier> fields, IndexCustomState customState)
        {
            return CreateState(table, name, new State(isPrimaryKey, fields, customState));
        }

        public static SubObjectState<UniqueIndexType, State> CreateUnique(Identifier table, string name, IEnumerable<Identifier> fields, IndexCustomState customState)
        {
            return Create(table, name, false, fields, customState);
        }

        public static SubObjectState<UniqueIndexType, State> CreatePrimaryKey(Identifier table, string name, IEnumerable<Identifier> fields, IndexCustomState customState)
        {
            return Create(table, name, true, fields, customState);
        }

        public static bool IndexHasChanged(SchemaChanges changes, SubObjectState<UniqueIndexType, UniqueIndexType.State> current)
        {
            if (current == null) return true;

            var desired = changes.Desired.Get(current);

            // The index doesn't exist in the desired schema at all
            if (desired == null) return true;

            // It's changed from primary key to not or vice versa
            if (desired.State.IsPrimaryKey != current.State.IsPrimaryKey) return true;

            // The index fields are different
            if (IndexState.IndexStateHasChanged(changes, current.ParentIdentifier, current.State.IndexState, desired.State.IndexState)) return true;

            return false;
        }
    }
}
