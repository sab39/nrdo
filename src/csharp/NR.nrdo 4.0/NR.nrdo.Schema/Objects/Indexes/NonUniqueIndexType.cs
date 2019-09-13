using System.Linq;
using System.Collections.Generic;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;

namespace NR.nrdo.Schema.Objects.Indexes
{
    public sealed class NonUniqueIndexType : SubObjectType<NonUniqueIndexType, IndexState, TableType, Stateless>
    {
        public override string Name { get { return "index"; } }

        public override IEnumerable<SubObjectState<NonUniqueIndexType, IndexState>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from index in connection.GetAllNonUniqueIndexes()
                   join custom in connection.GetAllIndexCustomState()
                        on new { table = TableType.Identifier(index.Table.QualifiedName), index = NonUniqueIndexType.Identifier(index.Name) }
                        equals new { table = TableType.Identifier(custom.Table.QualifiedName), index = NonUniqueIndexType.Identifier(custom.IndexName) }
                        into indexCustom
                   select Create(TableType.Identifier(index.Table.QualifiedName), index.Name,
                                 from field in index.FieldNames select FieldType.Identifier(field),
                                 indexCustom.Any() ? indexCustom.Single().CustomState :
                                 connection.SchemaDriver.DefaultIndexCustomState);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new DropIndexesStep();
                yield return new AddIndexesStep();
            }
        }
 
        public static SubObjectState<NonUniqueIndexType, IndexState> Create(Identifier table, string name, IEnumerable<Identifier> fields, IndexCustomState customState)
        {
            return CreateState(table, name, new IndexState(fields, customState));
        }

        public static bool IndexHasChanged(SchemaChanges changes, SubObjectState<NonUniqueIndexType, IndexState> current)
        {
            var desired = changes.Desired.Get(current);

            // The index doesn't exist in the desired schema at all
            if (desired == null) return true;

            // The index fields are different
            if (IndexState.IndexStateHasChanged(changes, current.ParentIdentifier, current.State, desired.State)) return true;

            return false;
        }    
    }
}
