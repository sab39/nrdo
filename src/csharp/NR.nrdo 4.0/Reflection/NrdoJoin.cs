using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoJoin : IComparable<NrdoJoin>, IDfnElement
    {
        internal NrdoJoin(NrdoGetBase get, NrdoJoinAttribute jattr)
        {
            this.get = get;
            from = new NrdoFieldRef(get.getFromTableByAlias(jattr.FromTable), jattr.FromField);
            to = new NrdoFieldRef(get.getToTableByAlias(jattr.ToTable), jattr.ToField);
            index = jattr.Index;
        }

        private NrdoGetBase get;

        private int index;

        private NrdoFieldRef from;
        public NrdoFieldRef From { get { return from; } }

        private NrdoFieldRef to;
        public NrdoFieldRef To { get { return to; } }

        internal bool isSelfToTarget { get { return From.Table.IsSelf && get.IsTargetTable(To.Table); } }

        string IDfnElement.ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("      ");
            sb.Append(From.Table.IsSelf ? "*" : From.Table.Alias);
            sb.Append(" to ");
            sb.Append(get.IsTargetTable(To.Table) ? "*" : To.Table.Alias);
            sb.Append(" {" + From.Field.Name + " " + To.Field.Name + "}");
            return sb.ToString();
        }

        public int CompareTo(NrdoJoin other)
        {
            return index.CompareTo(other.index);
        }
    }
}
