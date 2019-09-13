using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NR.nrdo.Attributes;
using System.Linq;

namespace NR.nrdo.Reflection
{
    public abstract class NrdoObjectType
    {
        internal NrdoObjectType(Type type, NrdoObjectTypeAttribute attr)
        {
            this.Type = type;
            this.Name = attr.Name;
            this.CacheFileName = attr.CacheFileName;
            this.CacheFileContents = attr.CacheFileContents;
        }

        public string Name { get; private set; }

        public virtual string DatabaseName { get { return Name.Replace(':', '_'); } }

        public Type Type { get; private set; }
        public string CacheFileName { get; private set; }
        public string CacheFileContents { get; private set; }

        public string Module
        {
            get
            {
                int pos = Name.LastIndexOf(':');
                return pos < 0 ? null : Name.Substring(0, pos);
            }
        }
        public string UnqualifiedName
        {
            get
            {
                int pos = Name.LastIndexOf(':');
                return pos < 0 ? Name : Name.Substring(pos + 1);
            }
        }

        private List<NrdoBeforeStatement> beforeStatements;
        public IList<NrdoBeforeStatement> BeforeStatements
        {
            get
            {
                if (beforeStatements == null)
                {
                    beforeStatements = (from attr in Type.GetAttributes<NrdoBeforeStatementAttribute>()
                                       select new NrdoBeforeStatement(attr)).ToList();
                    beforeStatements.Sort();
                }
                return beforeStatements;
            }
        }
    }
}
