using System;
using System.Collections.Generic;
using System.Text;
using NR.nrdo.Reflection;

namespace NR.nrdo.Loader
{
	class RecordKey
	{
        public RecordKey(NrdoTable table, FieldValueGetter getValue)
        {
            this.table = table;
            this.getValue = getValue;
        }
        public RecordKey(RecipeRecord record)
        {
            this.table = record.Table;
            this.getValue = delegate(NrdoField field)
            {
                RecipeValueField rfield = record.GetField(field.Name) as RecipeValueField;
                return rfield == null ? Recipe.defaultValue(field.Type, field.IsNullable) : rfield.Value;
            };
        }
        public RecordKey(ITableObject data)
        {
            this.table = NrdoTable.GetTable(data.GetType());
            this.getValue = delegate(NrdoField field)
            {
                if (data.IsNew && table.IsPkeySequenced && field == table.PkeyGet.Fields[0].Field)
                {
                    return Undefined.Value;
                }
                else
                {
                    return field.Get(data);
                }
            };
        }

        private readonly NrdoTable table;
        public NrdoTable Table { get { return table; } }

        private readonly FieldValueGetter getValue;
        internal object GetValue(NrdoField field)
        {
            return getValue(field);
        }

        public bool IsDefined
        {
            get
            {
                foreach (NrdoFieldRef field in table.PkeyGet.Fields)
                {
                    if (getValue(field.Field) is Undefined) return false;
                }
                return true;
            }
        }

        public override bool Equals(object obj)
        {
            RecordKey key = obj as RecordKey;
            if (key == null) return false;

            if (key.table.Name != table.Name) return false;

            foreach (NrdoFieldRef field in table.PkeyGet.Fields)
            {
                if (!object.Equals(getValue(field.Field), key.getValue(field.Field))) return false;
            }
            return true;
        }
        private int hash(object value)
        {
            return value == null ? 0 : value.GetHashCode();
        }
        public override int GetHashCode()
        {
            int hashCode = table.Name.GetHashCode();
            foreach (NrdoFieldRef field in table.PkeyGet.Fields)
            {
                hashCode ^= field.Field.Name.GetHashCode() ^ hash(getValue(field.Field));
            }
            return hashCode;
        }
        public override string ToString()
        {
            string result = null;
            foreach (NrdoFieldRef field in table.PkeyGet.Fields)
            {
                if (result != null) result += ", ";
                result += field.Field.Name + "=" + getValue(field.Field);
            }
            return table.Name + "(" + result + ")";
        }
    }
}
