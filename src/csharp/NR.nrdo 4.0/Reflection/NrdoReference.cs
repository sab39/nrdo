using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoReference : NrdoGetBase, IComparable<NrdoReference>
    {
        // Cannot be subclassed outside this assembly
        internal NrdoReference(NrdoTable table, MethodInfo method, NrdoRefAttribute rattr)
            : base(table, method, rattr)
        {
            this.index = rattr.Index;
            this.isFkey = rattr.Fkey;
            this.fkeyName = rattr.FkeyName;
            this.isCascadingFkey = rattr.Cascade;
            MethodInfo meth = targetTable.Type.GetMethod(rattr.Get, rattr.GetParams);
            foreach (NrdoGet get in targetTable.Gets)
            {
                if (get.Method.Equals(meth)) associatedGet = get;
            }
            if (associatedGet == null) throw new ArgumentException("Associated get for reference not found");
        }

        private int index;

        internal override string DfnSyntaxPrefix { get { return "references"; } }
        internal override string DfnSyntaxTarget { get { return TargetTable.Name; } }
        internal override string DfnSyntaxExtraLines
        {
            get
            {
                if (IsFkey)
                {
                    return "    fkey" + (IsCascadingFkey ? " cascade" : null) + ";\r\n";
                }
                else
                {
                    return null;
                }
            }
        }

        internal override bool IsTargetTable(NrdoTableRef table)
        {
            return table == TargetTableRef;
        }

        internal override NrdoTableRef getToTableByAlias(string alias)
        {
            if (alias == null) return TargetTableRef;

            return base.getToTableByAlias(alias);
        }
        internal override string DefaultName
        {
            get
            {
                string baseDefault = base.DefaultName;
                string targetName = TargetTable.Name;
                // This next line is cunning because it still works if the index is -1
                targetName = targetName.Substring(targetName.LastIndexOf(':') + 1);
                if (IsMulti) targetName += "s";
                if (baseDefault == null)
                {
                    return targetName;
                }
                else
                {
                    return targetName + "_by_" + baseDefault;
                }
            }
        }

        private NrdoTable targetTable;
        public NrdoTable TargetTable
        {
            get
            {
                if (targetTable == null)
                {
                    targetTable = NrdoTable.GetTable(((NrdoRefAttribute)attr).TargetType);
                }
                return targetTable;
            }
        }

        private NrdoGet associatedGet;
        public NrdoGet AssociatedGet { get { return associatedGet; } }

        private bool isFkey;
        public bool IsFkey { get { return isFkey; } }

        private bool isCascadingFkey;
        public bool IsCascadingFkey { get { return isCascadingFkey; } }

        private string fkeyName;
        public string FkeyName { get { return fkeyName; } }

        private NrdoTableRef targetTableRef;
        public NrdoTableRef TargetTableRef
        {
            get
            {
                if (targetTableRef == null) targetTableRef = new NrdoTableRef(TargetTable);
                return targetTableRef;
            }
        }

        public int CompareTo(NrdoReference other)
        {
            return index.CompareTo(other.index);
        }
    }

    public sealed class NrdoSingleReference : NrdoReference
    {
        internal NrdoSingleReference(NrdoTable table, MethodInfo method, NrdoRefAttribute rattr)
            : base(table, method, rattr) { }

        public override bool IsMulti { get { return false; } }
        public ITableObject Call(ITableObject obj, params object[] args)
        {
            if (!HasCode) throw new InvalidOperationException(Table.Name + " get " + Name + " has no code, so cannot be called");
            return (ITableObject)Method.Invoke(obj, args);
        }
        public NrdoSingleGet AssociatedSingleGet { get { return (NrdoSingleGet) AssociatedGet; } }
    }

    public sealed class NrdoMultiReference : NrdoReference
    {
        internal NrdoMultiReference(NrdoTable table, MethodInfo method, NrdoRefAttribute rattr)
            : base(table, method, rattr) { }

        public override bool IsMulti { get { return true; } }
        public IList<ITableObject> Call(ITableObject obj, params object[] args)
        {
            if (!HasCode) throw new InvalidOperationException(Table.Name + " get " + Name + " has no code, so cannot be called");
            var objects = (System.Collections.IList)Method.Invoke(obj, args);
            return objects.Cast<ITableObject>().ToList().AsReadOnly();
        }
        public NrdoMultiGet AssociatedMultiGet { get { return (NrdoMultiGet) AssociatedMultiGet; } }
    }
}
