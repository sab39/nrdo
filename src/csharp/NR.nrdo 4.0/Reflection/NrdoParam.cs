using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoParam : NrdoParamBase, IDfnElement, IComparable<NrdoParam>
    {
        public NrdoParam(NrdoParamAttribute attr)
        {
            this.index = attr.Index;
            this.name = attr.Name;
            this.isNullable = attr.Nullable;
            this.type = attr.Type;
            this.dbType = attr.DbType;
        }

        private int index;

        private string name;
        public override string Name { get { return name; } }

        private bool isNullable;
        public override bool IsNullable { get { return isNullable; } }

        private Type type;
        public override Type Type { get { return type; } }

        private string dbType;
        public string DbType { get { return dbType; } }

        string IDfnElement.ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("      " + NrdoReflection.GetTypeString(Type) + " " + Name + " ");
            if (DbType != null) sb.Append(DbType + " ");
            sb.Append(IsNullable ? "nullable" : "notnull");
            sb.Append(" []");
            return sb.ToString();
        }

        int IComparable<NrdoParam>.CompareTo(NrdoParam other)
        {
            return index.CompareTo(other.index);
        }
    }
}
