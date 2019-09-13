using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Caching;
using NR.nrdo.Connection;

namespace NR.nrdo
{
    public abstract class Where<T>
        where T : DBObject<T>
    {
        public abstract void SetOnCmd(NrdoCommand cmd);
        public abstract string SQLStatement { get; }
        public abstract string GetMethodName { get; }
        public virtual IDBObjectCache<T> Cache { get { return null; } }
        public virtual string GetParameters { get { return ""; } }
        public virtual bool IsStoredProc { get { return false; } }
        public CommandType CommandType { get { return IsStoredProc ? CommandType.StoredProcedure : CommandType.Text; } }
    }
}
