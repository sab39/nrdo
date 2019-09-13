using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Sql
{
    public class SqlValueParam
    {
        private readonly DbType dbType;
        private readonly int len;
        private readonly object value;
        private readonly string rawSql;

        private SqlValueParam(DbType dbType, int len, object value, string rawSql)
        {
            this.dbType = dbType;
            this.len = len;
            this.value = value;
            this.rawSql = rawSql;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SqlValueParam);
        }
        public bool Equals(SqlValueParam fp)
        {
            if (fp == null) return false;
            return dbType == fp.dbType &&
                len == fp.len &&
                object.Equals(value, fp.value) &&
                rawSql == fp.rawSql;
        }

        public override int GetHashCode()
        {
            return value == null ? dbType.GetHashCode() : value.GetHashCode();
        }

        public void SetParameter(NrdoCommand cmd, string name)
        {
            cmd.SetParameter(name, dbType, len, value);
        }

        private static string numToString<T>(T? value) where T : struct
        {
            return value == null ? "Null" : value.ToString();
        }

        public static SqlValueParam ForInt(int? value)
        {
            return new SqlValueParam(DbType.Int32, Nint.Len(value), value, numToString(value));
        }
        public static SqlValueParam ForUint(uint? value)
        {
            return new SqlValueParam(DbType.UInt32, Nuint.Len(value), value, numToString(value));
        }
        public static SqlValueParam ForLong(long? value)
        {
            return new SqlValueParam(DbType.Int64, Nlong.Len(value), value, numToString(value));
        }
        public static SqlValueParam ForBool(bool? value)
        {
            return new SqlValueParam(DbType.Boolean, Nbool.Len(value), value, value == null ? "Null" : value.Value ? "1" : "0");
        }
        public static SqlValueParam ForString(string value)
        {
            return new SqlValueParam(DbType.String, Nstring.Len(value), value, null);
        }
        public static SqlValueParam ForDateTime(DateTime? value)
        {
            return new SqlValueParam(DbType.DateTime, NDateTime.Len(value), value, null);
        }
        public static SqlValueParam ForDecimal(decimal? value)
        {
            return new SqlValueParam(DbType.Decimal, Ndecimal.Len(value), value, numToString(value));
        }
        public static SqlValueParam ForGuid(Guid? value)
        {
            return new SqlValueParam(DbType.Guid, 16, value, null);
        }
        /// <summary>
        /// This is for values that cannot really be used as an SQL value. We use a "faked"
        /// SqlValueParam to make it possible for unsupported types to propagate their values through
        /// the same APIs that other types use, but the code on the other end must be aware of this
        /// and treat them specially
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static SqlValueParam ForUnsupportedValue(object value)
        {
            return new SqlValueParam((DbType)(-1), 0, value, null);
        }

        public object GetObjectValue()
        {
            return value;
        }

        public string ToRawSql()
        {
            if (rawSql == null) throw new ApplicationException("Cannot convert value of " + dbType + " to raw SQL safely");
            return rawSql;
        }
    }
}
