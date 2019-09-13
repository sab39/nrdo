using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoFieldRef : NrdoParamBase, IComparable<NrdoFieldRef>, IDfnElement
    {
        internal NrdoFieldRef(NrdoGetBase get, NrdoByFieldAttribute fattr)
            : this(get.getTableByAlias(fattr.Table), fattr.FieldName)
        {
            index = fattr.Index;
        }
        internal NrdoFieldRef(NrdoTableRef table, string field)
        {
            this.table = table;
            this.field = table.Table.GetField(field);
        }

        private NrdoTableRef table;
        public NrdoTableRef Table { get { return table; } }

        private NrdoField field;
        public NrdoField Field { get { return field; } }

        private int index;

        public override string Name { get { return isSelf ? Field.Name : Table.Alias + "_" + Field.Name; } }
        public override Type Type { get { return Field.Type; } }
        public override bool IsNullable { get { return true; } }

        string IDfnElement.ToDfnSyntax()
        {
            return isSelf ? Field.Name : Table.Alias + "." + Field.Name;
        }
        private bool isSelf { get { return Table.IsSelf || Table.IsTarget; } }

        public int CompareTo(NrdoFieldRef other)
        {
            return index.CompareTo(other.index);
        }
    }
}
