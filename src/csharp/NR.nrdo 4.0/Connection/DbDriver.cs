using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public abstract class DbDriver
    {
        #region Metadata
        
        public abstract string Identifier { get; }
        public abstract string DisplayName { get; }

        #endregion

        #region Creating connection objects

        public abstract IDbConnection CreateConnection(string connectionString);

        protected abstract IDbDataParameter CreateParameter(string name);
        public virtual IDbDataParameter CreateParameter(string name, DbType type, int len, object value)
        {
            var param = CreateParameter(name);
            param.DbType = type;
            param.Size = len;
            param.Value = value ?? DBNull.Value;
            return param;
        }

        #endregion

        #region Identifier quoting

        public virtual string ExtractSchema(ref string name)
        {
            int pos = name.IndexOf('.');
            if (pos >= 0)
            {
                var schema = name.Substring(0, pos);
                name = name.Substring(pos + 1);
                return schema;
            }
            else
            {
                return null;
            }
        }

        // How are identifiers quoted
        public virtual string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier + "\"";
        }

        public virtual string QuoteSchemaIdentifier(string name)
        {
            var schema = ExtractSchema(ref name);
            return (schema == null ? null : QuoteIdentifier(schema) + ".") + QuoteIdentifier(name);
        }

        #endregion

        #region String comparison

        public bool StringEquals(string a, string b)
        {
            return DbStringComparer.Equals(a, b);
        }

        public virtual IEqualityComparer<string> DbStringComparer
        {
            get { return StringComparer.OrdinalIgnoreCase; }
        }

        #endregion

        #region SQL Syntax

        // What are the correct substitutions for ::true and ::false?
        public virtual string TrueSql
        {
            get { return "true"; } // or "1" or "-1" or "'Y'"
        }

        public virtual string FalseSql
        {
            get { return "false"; } // or "0" or "'N'"
        }

        // The SQL standard uses || for concat, SQL server uses +
        public virtual string ConcatSql
        {
            get { return "||"; } // or "+"
        }

        public abstract string NowSql { get; } // No portable answer

        public abstract string TodaySql { get; } // No portable answer

        public virtual string GetSelectFromNothingSql(string selectClause)
        {
            return "SELECT " + selectClause;
        }

        public abstract string GetNewSequencedKeyValueSql(string sequenceName);

        #endregion

        #region Data types

        // The unsigned types and the signed byte type (uint, ushort, ulong, sbyte) do not have corresponding
        // get methods in IDataReader so they cannot easily be supported. All except UInt64 could be handled
        // by downcasting from the next signed size up, but that is extra effort for something that's never even
        // been attempted to be used.

        // bool -> Boolean
        public virtual IDataParameter CreateBoolParameter(string name, string dataType, bool? value)
        {
            return CreateParameter(name, DbType.Boolean, 1, value);
        }

        public virtual bool ReadBoolValue(IDataReader dr, int index)
        {
            return dr.GetBoolean(index);
        }

        // byte -> Byte
        public virtual IDataParameter CreateByteParameter(string name, string dataType, byte? value)
        {
            return CreateParameter(name, DbType.Byte, 1, value);
        }

        public virtual byte ReadByteValue(IDataReader dr, int index)
        {
            return dr.GetByte(index);
        }

        // char -> dbtype StringFixedLength, IDataType Char
        public virtual IDataParameter CreateCharParameter(string name, string dataType, char? value)
        {
            return CreateParameter(name, DbType.StringFixedLength, 1, value);
        }

        public virtual char ReadCharValue(IDataReader dr, int index)
        {
            return dr.GetChar(index);
        }

        // short -> Int16
        public virtual IDataParameter CreateShortParameter(string name, string dataType, short? value)
        {
            return CreateParameter(name, DbType.Int16, 2, value);
        }

        public virtual short ReadShortValue(IDataReader dr, int index)
        {
            return dr.GetInt16(index);
        }

        // int -> Int32
        public virtual IDataParameter CreateIntParameter(string name, string dataType, int? value)
        {
            return CreateParameter(name, DbType.Int32, 4, value);
        }

        public virtual int ReadIntValue(IDataReader dr, int index)
        {
            return dr.GetInt32(index);
        }

        // long -> Int64
        public virtual IDataParameter CreateLongParameter(string name, string dataType, long? value)
        {
            return CreateParameter(name, DbType.Int64, 8, value);
        }

        public virtual long ReadLongValue(IDataReader dr, int index)
        {
            return dr.GetInt64(index);
        }

        // float -> dbtype Single, IDataType Float
        public virtual IDataParameter CreateFloatParameter(string name, string dataType, float? value)
        {
            return CreateParameter(name, DbType.Single, 4, value);
        }

        public virtual float ReadFloatValue(IDataReader dr, int index)
        {
            return dr.GetFloat(index);
        }

        // double -> Double
        public virtual IDataParameter CreateDoubleParameter(string name, string dataType, double? value)
        {
            return CreateParameter(name, DbType.Double, 8, value);
        }

        public virtual double ReadDoubleValue(IDataReader dr, int index)
        {
            return dr.GetDouble(index);
        }

        // decimal -> dbtype (if sqltype = $dbmoneytype$ then Currency else Decimal), IDataType Decimal
        public virtual IDataParameter CreateDecimalParameter(string name, string dataType, decimal? value)
        {
            var dbType = DbType.Decimal;
            if (StringEquals(dataType, "money") || StringEquals(dataType, "currency")) dbType = DbType.Currency;

            return CreateParameter(name, dbType, 9, value);
        }

        public virtual decimal ReadDecimalValue(IDataReader dr, int index)
        {
            return dr.GetDecimal(index);
        }

        // DateTime -> DateTime
        public virtual IDataParameter CreateDateTimeParameter(string name, string dbType, DateTime? value)
        {
            return CreateParameter(name, DbType.DateTime, 8, value);
        }

        public virtual DateTime ReadDateTimeValue(IDataReader dr, int index)
        {
            return dr.GetDateTime(index);
        }

        // string -> String
        public virtual IDataParameter CreateStringParameter(string name, string dataType, string value)
        {
            var len = value == null ? 1 : value.Length;
            if (len < 2048)
            {
                var pow2 = 1;
                while (pow2 < len) pow2 *= 2;
                len = pow2;
            }
            return CreateParameter(name, DbType.String, len, value);
        }

        public virtual string ReadStringValue(IDataReader dr, int index)
        {
            return dr.GetString(index);
        }

        #endregion
    }
}
