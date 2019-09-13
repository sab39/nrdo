using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using NR.nrdo.Schema.Tool;
using NR.nrdo.Schema.Objects.Fields;
using NR.nrdo.Schema.Drivers;

namespace NR.nrdo.Schema.Objects.Indexes
{
    public sealed class IndexState
    {
        private readonly ReadOnlyCollection<string> fieldNames;
        public IEnumerable<string> FieldNames { get { return fieldNames; } }

        public IEnumerable<Identifier> Fields { get { return from field in fieldNames select FieldType.Identifier(field); } }

        private readonly IndexCustomState customState;
        public IndexCustomState CustomState { get { return customState; } }

        internal IndexState(IEnumerable<string> fieldNames, IndexCustomState customState)
        {
            this.fieldNames = fieldNames.ToList().AsReadOnly();
            this.customState = customState;
        }
        internal IndexState(IEnumerable<Identifier> fields, IndexCustomState customState)
            : this(from field in fields select field.Name, customState)
        {
            foreach (var field in fields) FieldType.CheckType(field);
        }

        public override string ToString()
        {
            return "index fields: (" + string.Join(", ", fieldNames) + ") " + customState;
        }

        internal static bool IndexStateHasChanged(SchemaChanges changes, Identifier table, IndexState current, IndexState desired)
        {
            // The table might need to be dropped entirely to reorder the columns
            if (FieldType.IsFieldReorderPossiblyNeeded(changes, table)) return true;

            // The sequence of field names don't match
            if (!Enumerable.SequenceEqual(desired.FieldNames, current.FieldNames, changes.DbDriver.DbStringComparer)) return true;

            // Something custom about the index has changed
            if (!changes.SchemaDriver.IsIndexCustomStateEqual(current.CustomState, desired.CustomState)) return true;

            // Any of the fields in the index are changing
            if (current.Fields.Any(field => FieldType.FieldHasChanged(changes, table, field))) return true;

            return false;
        }    
    }
}
