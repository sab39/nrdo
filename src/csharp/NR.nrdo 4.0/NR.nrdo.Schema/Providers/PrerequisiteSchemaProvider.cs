using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Schema.Objects;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Objects.Indexes;
using NR.nrdo.Schema.Objects.Fkeys;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Shared;
using NR.nrdo.Schema.Objects.Internal;
using NR.nrdo.Util.OutputUtil;

namespace NR.nrdo.Schema.Providers
{
    public sealed class PrerequisiteSchemaProvider : IPrerequisiteSchemaProvider
    {
        public bool IncludeInNormalRun { get { return true; } }

        public IEnumerable<ObjectType> GetObjectTypes(SchemaDriver schemaDriver)
        {
            // Types that are required for the creation of the tables that nrdo uses to track its own state.
            // Note: Do not remove any types from this list that have ever been in it:
            // any types that have ever been here need to be able to drop themselves on upgrades from versions that
            // used them, even if they are no longer used for these tables.
            yield return TableType.Instance;
            yield return FieldType.Instance;
            yield return UniqueIndexType.Instance;
            yield return FkeyType.Instance;
        }

        public IEnumerable<ObjectState> GetDesiredState(SchemaConnection connection, IOutput output)
        {
            var schemaDriver = connection.SchemaDriver;

            var stateTable = TableType.Create("dbo.nrdo_object");
            var stateTypeField = FieldType.Create(stateTable.Identifier, "type", 0, "nvarchar(100)", false);
            var stateNameField = FieldType.Create(stateTable.Identifier, "name", 1, "nvarchar(500)", false);
            var statePk = UniqueIndexType.CreatePrimaryKey(stateTable.Identifier, "nrdo_object_pk",
                new[] { stateTypeField.Identifier, stateNameField.Identifier },
                schemaDriver.DefaultPrimaryKeyCustomState);

            var subTable = TableType.Create("dbo.nrdo_object_sub");
            var subParentTypeField = FieldType.Create(subTable.Identifier, "parent_type", 0, "nvarchar(100)", false);
            var subParentNameField = FieldType.Create(subTable.Identifier, "parent_name", 1, "nvarchar(500)", false);
            var subTypeField = FieldType.Create(subTable.Identifier, "type", 2, "nvarchar(100)", false);
            var subNameField = FieldType.Create(subTable.Identifier, "name", 3, "nvarchar(500)", false);
            var subPk = UniqueIndexType.CreatePrimaryKey(subTable.Identifier, "nrdo_object_sub_pk", 
                new[] { subParentTypeField.Identifier, subParentNameField.Identifier, subTypeField.Identifier, subNameField.Identifier },
                schemaDriver.DefaultPrimaryKeyCustomState);
            var subFk = FkeyType.Create(subTable.Identifier, stateTable.Identifier, "nrdo_object_sub_parent_fk", true, new[] {
                new FieldPair(subParentTypeField.Name, stateTypeField.Name),
                new FieldPair(subParentNameField.Name, stateNameField.Name),
            });

            return new ObjectState[] {
                stateTable, stateTypeField, stateNameField, statePk, subTable, subParentTypeField, subParentNameField, subTypeField, subNameField, subPk, subFk
            };
        }
    }
}
