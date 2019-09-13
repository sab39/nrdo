using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;

namespace NR.nrdo.Schema.Shared
{
    public sealed class FieldPair
    {
        private readonly string fromFieldName;
        public string FromFieldName { get { return fromFieldName; } }

        private readonly string toFieldName;
        public string ToFieldName { get { return toFieldName; } }

        public FieldPair(string fromFieldName, string toFieldName)
        {
            if (string.IsNullOrWhiteSpace(fromFieldName)) throw new ArgumentNullException("fromFieldName");
            if (string.IsNullOrWhiteSpace(toFieldName)) throw new ArgumentNullException("toFieldName");

            this.fromFieldName = fromFieldName;
            this.toFieldName = toFieldName;
        }

        public override string ToString()
        {
            return fromFieldName + "=" + toFieldName;
        }

        public static IEqualityComparer<FieldPair> GetComparer(DbDriver dbDriver)
        {
            return new FieldPairComparer(dbDriver);
        }

        private class FieldPairComparer : IEqualityComparer<FieldPair>
        {
            private readonly DbDriver dbDriver;
            internal FieldPairComparer(DbDriver dbDriver)
            {
                this.dbDriver = dbDriver;
            }

            public bool Equals(FieldPair a, FieldPair b)
            {
                return dbDriver.StringEquals(a.FromFieldName, b.FromFieldName) &&
                    dbDriver.StringEquals(a.ToFieldName, b.ToFieldName);
            }

            public int GetHashCode(FieldPair fp)
            {
                return dbDriver.DbStringComparer.GetHashCode(fp.ToString());
            }
        }        
    }
}
