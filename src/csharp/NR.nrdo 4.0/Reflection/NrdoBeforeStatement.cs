using System;
using System.Collections.Generic;
using System.Text;
using NR.nrdo.Attributes;

namespace NR.nrdo.Reflection
{
    public class NrdoBeforeStatement : IComparable<NrdoBeforeStatement>, IDfnElement
    {
        internal NrdoBeforeStatement(NrdoBeforeStatementAttribute attr)
        {
            this.index = attr.Index;
            this.name = attr.Name;
            this.step = attr.Step;
            this.statement = attr.Statement;
            this.initial = attr.Initial;
            this.upgrade = attr.Upgrade;
        }
        int index;

        private string name;
        public string Name { get { return name; } }

        private string step;
        public string Step { get { return step; } }
 
        private string statement;
        public string Statement { get { return statement; } }

        private bool initial;
        public bool Initial { get { return initial; } }

        private bool upgrade;
        public bool Upgrade { get { return upgrade; } }

        int IComparable<NrdoBeforeStatement>.CompareTo(NrdoBeforeStatement other)
        {
            return index.CompareTo(other.index);
        }

        string IDfnElement.ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("  before ");
            if (Initial && !Upgrade) sb.Append("initially ");
            if (Upgrade && !Initial) sb.Append("upgrade ");
            sb.Append(Step + " " + Name + " by [" + Statement.Replace("$", "$$").Replace("[", "[[").Replace("]", "[]") + "]");
            return sb.ToString();
        }
    }
}
