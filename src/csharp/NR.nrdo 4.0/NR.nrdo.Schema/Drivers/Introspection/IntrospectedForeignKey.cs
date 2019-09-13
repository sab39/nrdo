using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using NR.nrdo.Schema.Shared;

namespace NR.nrdo.Schema.Drivers.Introspection
{
    public class IntrospectedForeignKey
    {
        private readonly IntrospectedTable fromTable;
        public IntrospectedTable FromTable { get { return fromTable; } }

        private readonly IntrospectedTable toTable;
        public IntrospectedTable ToTable { get { return toTable; } }

        private readonly string name;
        public string Name { get { return name; } }

        private readonly bool isCascadeDelete;
        public bool IsCascadeDelete { get { return isCascadeDelete; } }

        private readonly ReadOnlyCollection<FieldPair> joins;
        public IEnumerable<FieldPair> Joins { get { return joins; } }

        public IntrospectedForeignKey(IntrospectedTable fromTable, IntrospectedTable toTable, string name, bool isCascadeDelete, IEnumerable<FieldPair> joins)
        {
            this.fromTable = fromTable;
            this.toTable = toTable;
            this.name = name;
            this.isCascadeDelete = isCascadeDelete;
            this.joins = joins.ToList().AsReadOnly();
        }
    }
}
