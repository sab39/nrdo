using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NR.nrdo.Connection;

namespace NR.nrdo.Schema.Shared
{
    public sealed class ProcParam
    {
        private readonly string name;
        public string Name { get { return name; } }

        private readonly string dataType;
        public string DataType { get { return dataType; } }

        public ProcParam(string name, string dataType)
        {
            this.name = name;
            this.dataType = dataType;
        }

        public override string ToString()
        {
            return name + " " + dataType;
        }

        public static IEqualityComparer<ProcParam> GetComparer(DbDriver dbDriver)
        {
            return new ParameterComparer(dbDriver);
        }

        private class ParameterComparer : IEqualityComparer<ProcParam>
        {
            private readonly DbDriver dbDriver;
            internal ParameterComparer(DbDriver dbDriver)
            {
                this.dbDriver = dbDriver;
            }

            public bool Equals(ProcParam a, ProcParam b)
            {
                return dbDriver.StringEquals(a.Name, b.Name) &&
                    dbDriver.StringEquals(a.DataType, b.DataType);
            }

            public int GetHashCode(ProcParam param)
            {
                return dbDriver.DbStringComparer.GetHashCode(param.ToString());
            }
        }    
    }
}
