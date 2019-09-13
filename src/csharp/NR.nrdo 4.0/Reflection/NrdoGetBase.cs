using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using NR.nrdo.Attributes;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoGetBase : IDfnElement
    {
        // Cannot be subclassed outside this assembly
        internal NrdoGetBase(NrdoTable table, MethodInfo method, NrdoGetBaseAttribute gattr)
        {
            this.attr = gattr;
            this.table = table;
            this.method = method;
            this.name = gattr.Name;
            this.hasCode = gattr.HasCode;
            this.where = gattr.Where;

            tables = new List<NrdoTableRef>();
            foreach (var tattr in method.GetAttributes<NrdoByTableAttribute>())
            {
                if (tattr.Param)
                {
                    // FIXME: add to ParamTables
                }
                else
                {
                    tables.Add(new NrdoTableRef(tattr));
                }
            }
            tables.Sort();

            fields = (from fattr in method.GetAttributes<NrdoByFieldAttribute>()
                      orderby fattr.Index
                      select new NrdoFieldRef(this, fattr)).ToList();

            joins = (from jattr in method.GetAttributes<NrdoJoinAttribute>()
                     orderby jattr.Index
                     select new NrdoJoin(this, jattr)).ToList();

            @params = (from pattr in method.GetAttributes<NrdoParamAttribute>()
                       orderby pattr.Index
                       select new NrdoParam(pattr)).ToList();

            orderby = new List<NrdoOrderByClause>();
            foreach (var oattr in method.GetAttributes<NrdoOrderByAttribute>())
            {
                if (oattr.Field != null)
                {
                    orderby.Add(new NrdoOrderByField(this, oattr));
                }
                else
                {
                    orderby.Add(new NrdoOrderBySql(this, oattr));
                }
            }
            orderby.Sort();
        }
        protected NrdoGetBaseAttribute attr;

        public abstract bool IsMulti { get;}
        internal abstract string DfnSyntaxPrefix { get;}
        internal virtual string DfnSyntaxTarget { get { return null; } }
        internal virtual string DfnSyntaxExtraLines { get { return null; } }
        internal virtual bool IsTargetTable(NrdoTableRef table) { return false; }

        private NrdoTable table;
        public NrdoTable Table { get { return table; } }

        private string name;
        public string Name { get { return name; } }

        internal virtual string DefaultName
        {
            get
            {
                string result = null;
                foreach (NrdoParamBase param in AllParams)
                {
                    if (result != null) result += "_";
                    result += param.Name;
                }
                return result;
            }
        }

        private List<NrdoFieldRef> fields;
        public IList<NrdoFieldRef> Fields { get { return new ReadOnlyCollection<NrdoFieldRef>(fields); } }

        private List<NrdoTableRef> tables;
        public IList<NrdoTableRef> Tables { get { return new ReadOnlyCollection<NrdoTableRef>(tables); } }

        private List<NrdoJoin> joins;
        public IList<NrdoJoin> Joins { get { return new ReadOnlyCollection<NrdoJoin>(joins); } }

        private List<NrdoOrderByClause> orderby;
        public IList<NrdoOrderByClause> OrderBy { get { return new ReadOnlyCollection<NrdoOrderByClause>(orderby); } }

        private List<NrdoParam> @params;
        public IList<NrdoParam> Params { get { return new ReadOnlyCollection<NrdoParam>(@params); } }

        public IList<NrdoParamTable> ParamTables { get { return new List<NrdoParamTable>(); } }

        private List<NrdoParamBase> allParams;
        public IList<NrdoParamBase> AllParams
        {
            get
            {
                if (allParams == null)
                {
                    allParams = new List<NrdoParamBase>();
                    allParams.AddRange(ParamTables.Cast<NrdoParamBase>());
                    allParams.AddRange(Fields.Cast<NrdoParamBase>());
                    allParams.AddRange(Params.Cast<NrdoParamBase>());
                }
                return allParams.AsReadOnly();
            }
        }

        private string where;
        public string Where { get { return where; } }

        internal NrdoTableRef getTableByAlias(string alias)
        {
            return getToTableByAlias(alias);
        }
        internal NrdoTableRef getFromTableByAlias(string alias)
        {
            if (alias == null) return Table.SelfTableRef;

            foreach (NrdoTableRef table in Tables)
            {
                if (table.Alias == alias) return table;
            }
            throw new ArgumentOutOfRangeException("No table with alias '" + alias + "' found in get");
        }

        internal virtual NrdoTableRef getToTableByAlias(string alias)
        {
            return getFromTableByAlias(alias);
        }

        private MethodInfo method;
        public MethodInfo Method { get { return method; } }

        private bool hasCode;
        public bool HasCode { get { return hasCode; } }

        // References can't have noindex specified so this isn't part of the
        // NrdoGetBase public API (NrdoGet overrides it to be public).
        // But it's declared here because ToDfnSyntax needs it.
        internal virtual bool InternalHasIndex { get { return true; } }

        string IDfnElement.ToDfnSyntax()
        {
            StringBuilder sb = new StringBuilder();

            // The primary key is special.
            if (this == Table.PkeyGet)
            {
                if (Table.IsPkeySequenced)
                {
                    sb.Append("  pkey sequenced " + Fields[0].Name);
                }
                else
                {
                    sb.Append("  pkey {");
                    sb.Append(Fields.DfnSyntaxList());
                    sb.Append("}");
                }
                if (HasCode && InternalHasIndex && Name == DefaultName) return sb.ToString();
                sb.Append(" {\r\n");
            }
            else
            {
                sb.Append("  " + DfnSyntaxPrefix + " ");
                sb.Append(IsMulti ? "multi" : "single");
                if (DfnSyntaxTarget != null) sb.Append(" " + DfnSyntaxTarget);
                sb.Append(" {\r\n");
                sb.Append(DfnSyntaxExtraLines);
                if (Tables.Count > 0 || ParamTables.Count > 0)
                {
                    sb.Append("    tables {\r\n");
                    sb.Append(ParamTables.DfnSyntaxList(true));
                    sb.Append(Tables.DfnSyntaxList(true));
                    sb.Append("    };\r\n");
                }
                if (Joins.Count > 0)
                {
                    bool isBy = true;
                    foreach (NrdoJoin join in Joins)
                    {
                        if (!join.isSelfToTarget) isBy = false;
                    }
                    if (isBy)
                    {
                        sb.Append("    by {");
                        sb.Append(Joins.MapAndJoin("; ", join => join.From.Field.Name + " " + join.To.Field.Name));
                        sb.Append("};\r\n");
                    }
                    else
                    {
                        sb.Append(Joins.DfnSyntaxBlock("    joins", true));
                    }
                }
                sb.Append(Fields.DfnSyntaxBlock("    fields", false));
                sb.Append(Params.DfnSyntaxBlock("    params", true));
                if (Where != null)
                {
                    sb.Append("    where [" + Where + "];\r\n");
                }
                if (OrderBy.Count > 0)
                {
                    sb.Append(OrderBy.DfnSyntaxBlock("    orderby", false));
                }
            }
            if (!HasCode) sb.Append("    nocode;\r\n");
            if (!InternalHasIndex) sb.Append("    noindex;\r\n");
            if (Name != DefaultName) sb.Append("    called " + Name + ";\r\n");
            sb.Append("  }");
            return sb.ToString();
        }
    }
}
