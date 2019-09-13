using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public class NrdoTableRef : IComparable<NrdoTableRef>, IDfnElement
    {
        // Cannot be subclassed outside this assembly
        internal NrdoTableRef(NrdoByTableAttribute attr)
        {
            this.table = NrdoTable.GetTable(attr.Table);
            this.alias = attr.Alias;
            this.index = attr.Index;
        }
        internal NrdoTableRef(NrdoTable table)
        {
            this.table = table;
            this.alias = "self";
        }

        private int index;

        private NrdoTable table;
        public NrdoTable Table { get { return table; } }

        private string alias;
        public string Alias { get { return alias; } }

        public bool IsSelf { get { return this == Table.SelfTableRef; } }
        public bool IsTarget { get { return !IsSelf && alias == "self"; } }

        public int CompareTo(NrdoTableRef other)
        {
            return index.CompareTo(other.index);
        }

        string IDfnElement.ToDfnSyntax()
        {
            return "      " + Table.Name + " " + Alias;
        }
    }
}
