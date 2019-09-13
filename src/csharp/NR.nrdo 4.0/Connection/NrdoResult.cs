using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public struct NrdoResult
    {
        private readonly DbDriver dbDriver;
        private readonly IDataReader reader;
        private readonly RowCounter rowCounter;
        private readonly int rowNum;

        public DbDriver DbDriver { get { return dbDriver; } }
        public IDataReader Reader
        {
            get
            {
                checkValid();
                return reader;
            }
        }

        private class RowCounter
        {
            private int rowNum = 0;
            internal int currentRowNum { get { return rowNum; } }
            internal void advance()
            {
                rowNum++;
            }
        }

        private NrdoResult(DbDriver dbDriver, IDataReader reader, RowCounter rowCounter)
        {
            this.dbDriver = dbDriver;
            this.reader = reader;
            this.rowCounter = rowCounter;
            this.rowNum = rowCounter.currentRowNum;
        }

        internal static IEnumerable<NrdoResult> Get(DbDriver dbDriver, IDataReader reader)
        {
            var counter = new RowCounter();
            while (reader.Read())
            {
                yield return new NrdoResult(dbDriver, reader, counter);
                counter.advance();
            }
        }

        public bool IsValid
        {
            get { return !reader.IsClosed && rowCounter.currentRowNum == rowNum; }
        }

        private void checkValid()
        {
            if (!IsValid) throw new InvalidOperationException("Result cannot be accessed after reader has advanced to next row");
        }

        private T? getValue<T>(int index, Func<IDataReader, int, T> get)
            where T : struct
        {
            checkValid();
            return reader.IsDBNull(index) ? (T?)null : get(reader, index);
        }
        private T? getValue<T>(string colName, Func<IDataReader, int, T> get)
            where T : struct
        {
            return getValue(reader.GetOrdinal(colName), get);
        }

        public bool? GetBool(int index)
        {
            return getValue(index, dbDriver.ReadBoolValue);
        }
        public bool? GetBool(string colName)
        {
            return getValue(colName, dbDriver.ReadBoolValue);
        }

        public byte? GetByte(int index)
        {
            return getValue(index, dbDriver.ReadByteValue);
        }
        public byte? GetByte(string colName)
        {
            return getValue(colName, dbDriver.ReadByteValue);
        }

        public char? GetChar(int index)
        {
            return getValue(index, dbDriver.ReadCharValue);
        }
        public char? GetChar(string colName)
        {
            return getValue(colName, dbDriver.ReadCharValue);
        }

        public short? GetShort(int index)
        {
            return getValue(index, dbDriver.ReadShortValue);
        }
        public short? GetShort(string colName)
        {
            return getValue(colName, dbDriver.ReadShortValue);
        }

        public int? GetInt(int index)
        {
            return getValue(index, dbDriver.ReadIntValue);
        }
        public int? GetInt(string colName)
        {
            return getValue(colName, dbDriver.ReadIntValue);
        }

        public long? GetLong(int index)
        {
            return getValue(index, dbDriver.ReadLongValue);
        }
        public long? GetLong(string colName)
        {
            return getValue(colName, dbDriver.ReadLongValue);
        }

        public float? GetFloat(int index)
        {
            return getValue(index, dbDriver.ReadFloatValue);
        }
        public float? GetFloat(string colName)
        {
            return getValue(colName, dbDriver.ReadFloatValue);
        }

        public double? GetDouble(int index)
        {
            return getValue(index, dbDriver.ReadDoubleValue);
        }
        public double? GetDouble(string colName)
        {
            return getValue(colName, dbDriver.ReadDoubleValue);
        }

        public decimal? GetDecimal(int index)
        {
            return getValue(index, dbDriver.ReadDecimalValue);
        }
        public decimal? GetDecimal(string colName)
        {
            return getValue(colName, dbDriver.ReadDecimalValue);
        }

        public DateTime? GetDateTime(int index)
        {
            return getValue(index, dbDriver.ReadDateTimeValue);
        }
        public DateTime? GetDateTime(string colName)
        {
            return getValue(colName, dbDriver.ReadDateTimeValue);
        }

        public string GetString(int index)
        {
            checkValid();
            return reader.IsDBNull(index) ? null : dbDriver.ReadStringValue(reader, index);
        }
        public string GetString(string colName)
        {
            return GetString(reader.GetOrdinal(colName));
        }
    }
}
