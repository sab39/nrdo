using System;
using System.Collections.Generic;
using System.Linq;
using NR.nrdo.Schema.Drivers;
using NR.nrdo.Schema.Objects.Sequences;
using NR.nrdo.Schema.Objects.Tables;
using NR.nrdo.Schema.Objects.Triggers;
using NR.nrdo.Schema.State;
using NR.nrdo.Schema.Tool;
using System.Collections.Immutable;

namespace NR.nrdo.Schema.Objects.Fields
{
    public sealed class FieldType : SubObjectType<FieldType, FieldType.State, TableType, Stateless>
    {
        public override string Name { get { return "field"; } }

        public override IEnumerable<SubObjectState<FieldType, State>> GetExistingObjects(SchemaConnection connection, ObjectTypeHelper helper)
        {
            return from field in connection.GetAllFields()
                   let table = TableType.Identifier(field.Table.QualifiedName)
                   select field.IsSequencedPkey ? CreateSequencedPkey(table, field.Name, field.OrdinalPosition, field.DataType, field.IsNullable, field.SequenceName)
                                                : Create(table, field.Name, field.OrdinalPosition, field.DataType, field.IsNullable);
        }

        public override IEnumerable<StepBase> Steps
        {
            get
            {
                yield return new AlterFieldsStep();
                yield return new DropChangedFieldsStep();
                yield return new SetNullStep();
                yield return new AddFieldsStep();
                yield return new SetNotNullStep();
                yield return new DropFieldsStep();
            }
        }

        public sealed class State
        {
            private readonly int ordinalPosition;
            public int OrdinalPosition { get { return ordinalPosition; } }

            private readonly string dataType;
            public string DataType { get { return dataType; } }

            private readonly bool isNullable;
            public bool IsNullable { get { return isNullable; } }

            private readonly bool isSequencedPkey;
            public bool IsSequencedPkey { get { return isSequencedPkey; } }

            private readonly string sequenceName;
            public string SequenceName { get { return sequenceName; } }

            internal State(int ordinalPosition, string dataType, bool isNullable, bool isSequencedPkey, string sequenceName)
            {
                this.ordinalPosition = ordinalPosition;
                this.dataType = dataType;
                this.isNullable = isNullable;
                this.isSequencedPkey = isSequencedPkey;
                this.sequenceName = sequenceName;
            }

            public State WithTypeChange(string newDataType, bool newSequencedPkey, string newSequenceName)
            {
                if (Nstring.DBEquivalentComparer.Equals(dataType, newDataType) &&
                    newSequencedPkey == isSequencedPkey &&
                    Nstring.DBEquivalentComparer.Equals(sequenceName, newSequenceName)) return this;

                return new State(ordinalPosition, newDataType, isNullable, newSequencedPkey, newSequenceName);
            }

            public State WithNullable(bool newNullable)
            {
                if (isNullable == newNullable) return this;
                return new State(ordinalPosition, dataType, newNullable, isSequencedPkey, sequenceName);
            }

            public State WithOrdinalPosition(int newOrdinalPosition)
            {
                if (ordinalPosition == newOrdinalPosition) return this;
                return new State(newOrdinalPosition, dataType, isNullable, isSequencedPkey, sequenceName);
            }

            // We don't override Equals because field changes are done with checks for specific combinations of features, not just equality

            public override string ToString()
            {
                return dataType + (isSequencedPkey ? " sequenced" : "") + (isNullable ? " nullable" : " notnull");
            }
        }

        public static bool IsEqual(State a, State b, bool includeSequenced)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.IsNullable != b.IsNullable) return false;
            return IsTypeEqual(a, b, includeSequenced);
        }

        public static bool IsTypeEqual(State a, State b, bool includeSequenced)
        {
            if (object.ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (includeSequenced)
            {
                if (a.IsSequencedPkey != b.IsSequencedPkey || !Nstring.DBEquivalentComparer.Equals(a.SequenceName, b.SequenceName)) return false;
            }
            return Nstring.DBEquivalentComparer.Equals(a.DataType, b.DataType);
        }

        public static SubObjectState<FieldType, State> Create(Identifier table, string name, int ordinalPosition, string dataType, bool isNullable, bool isSequencedPkey = false)
        {
            return CreateState(table, name, new State(ordinalPosition, dataType, isNullable, isSequencedPkey, null));
        }

        public static SubObjectState<FieldType, State> CreateSequencedPkey(Identifier table, string name, int ordinalPosition, string dataType, bool isNullable, string sequenceName)
        {
            return CreateState(table, name, new State(ordinalPosition, dataType, isNullable, true, sequenceName));
        }

        public static bool FieldHasChanged(SchemaChanges changes, Identifier table, Identifier field)
        {
            var current = FieldType.GetFrom(changes.Current, table, field);
            var desired = FieldType.GetFrom(changes.Desired, table, field);
            return current == null || desired == null || !FieldType.IsEqual(current.State, desired.State, changes.SchemaDriver.IsSequencedPartOfFieldDeclaration);
        }

        public static SubObjectState<FieldType, State> GetDesired(SchemaChanges changes, SubObjectState<FieldType, State> current)
        {
            var desiredTable = TableType.GetDesiredIdentifier(changes, current.ParentIdentifier);
            if (desiredTable == null) return null;

            return GetFrom(changes.Desired, desiredTable, current.Identifier);
        }

        private static bool isFieldReorderNeededInternal(SchemaChanges changes, Identifier table, bool definite)
        {
            // Don't need a reorder if the table isn't order sensitive
            if (!FieldOrderSensitivityType.ChildrenFrom(changes.Desired, table).Any()) return false;

            // Get the current order of fields, ignoring any that will be dropped.
            var currentOrder = (from field in FieldType.ChildrenFrom(changes.Current, table)
                                where changes.Desired.Contains(field)
                                orderby field.State.OrdinalPosition
                                select field)
                               .ToImmutableList();

            // If there aren't any fields common between the two then we definitely don't need a reorder!
            if (!currentOrder.Any()) return false;

            // Get the desired order of fields (including any that haven't been added yet).
            var desiredOrder = from field in FieldType.ChildrenFrom(changes.Desired, table)
                               orderby field.State.OrdinalPosition
                               select field.Identifier;

            // Are the fields in the same order up to the end of the current fields? (New fields to be added at the end are fine)
            if (!Enumerable.SequenceEqual(from field in currentOrder select field.Identifier, desiredOrder.Take(currentOrder.Count))) return true;

            // Otherwise we don't know for sure, so if we're asking whether reorder is *definitely* needed, the answer is no.
            if (definite) return false;

            // Reorder MAY be needed if a field may be dropped-and-added due to a data type or identity change - except for the last field because it'd be
            // re-added in the same place.
            return currentOrder.Take(currentOrder.Count - 1)
                               .Any(field => !IsTypeEqual(field.State, changes.Desired.Get(field).State, changes.SchemaDriver.IsSequencedPartOfFieldDeclaration));
        }

        public static bool IsFieldReorderNeeded(SchemaChanges changes, Identifier table)
        {
            return isFieldReorderNeededInternal(changes, table, true);
        }

        public static bool IsFieldReorderPossiblyNeeded(SchemaChanges changes, Identifier table)
        {
            return isFieldReorderNeededInternal(changes, table, false);
        }
    }
}
