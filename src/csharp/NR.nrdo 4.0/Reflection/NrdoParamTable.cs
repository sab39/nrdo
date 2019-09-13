using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public sealed class NrdoParamTable : NrdoParamBase, IDfnElement
    {
        public NrdoParamTableRef TableRef { get { return null; } }
        public override string Name { get { return TableRef.Alias; } }
        public override Type Type { get { return TableRef.Table.Type; } }
        public override bool IsNullable { get { return false; } }

        string IDfnElement.ToDfnSyntax()
        {
            return "      " + TableRef.Table.Name + " " + TableRef.Alias + " []";
        }
    }
    public sealed class NrdoParamTableRef : NrdoTableRef 
    {
        private NrdoParamTableRef() : base((NrdoByTableAttribute) null) { }
        public NrdoParamTable Param { get { return null; } }
    }
}
