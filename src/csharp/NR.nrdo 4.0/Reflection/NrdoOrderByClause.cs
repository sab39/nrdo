using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoOrderByClause : IComparable<NrdoOrderByClause>, IDfnElement
    {
        // Cannot be subclassed outside this assembly
        internal NrdoOrderByClause(NrdoGetBase get, NrdoOrderByAttribute attr)
        {
            this.get = get;
            this.isDescending = attr.Descending;
            this.index = attr.Index;
        }

        private int index;

        private NrdoGetBase get;

        private bool isDescending;
        public bool IsDescending { get { return isDescending; } }

        string IDfnElement.ToDfnSyntax()
        {
            return IsDescending ? DfnSyntaxPrefix + " desc" : DfnSyntaxPrefix;
        }
        protected abstract string DfnSyntaxPrefix { get;}

        public int CompareTo(NrdoOrderByClause other)
        {
            return index.CompareTo(other.index);
        }
    }
    public sealed class NrdoOrderByField : NrdoOrderByClause
    {
        internal NrdoOrderByField(NrdoGetBase get, NrdoOrderByAttribute attr)
            : base(get, attr)
        {
            NrdoTableRef table = get.getTableByAlias(attr.Table);
            field = new NrdoFieldRef(table, attr.Field);
        }

        private NrdoFieldRef field;
        public NrdoFieldRef Field { get { return field; } }

        protected override string DfnSyntaxPrefix { get { return ((IDfnElement) Field).ToDfnSyntax(); } }
    }
    public sealed class NrdoOrderBySql : NrdoOrderByClause
    {
        internal NrdoOrderBySql(NrdoGetBase get, NrdoOrderByAttribute attr)
            : base(get, attr)
        {
            this.sql = attr.Sql;
        }
        private string sql;
        public string Sql { get { return sql; } }

        protected override string DfnSyntaxPrefix { get { return "[" + Sql + "] sql"; } }
    }
}
