using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NR.nrdo.Connection
{
    public struct NrdoCommand
    {
        private readonly DbDriver dbDriver;
        private readonly IDbCommand command;

        public DbDriver DbDriver { get { return dbDriver; } }
        public IDbCommand Command { get { return command; } }

        internal NrdoCommand(DbDriver dbDriver, IDbCommand command)
        {
            this.dbDriver = dbDriver;
            this.command = command;
        }

        public void SetParameter(string name, DbType dbType, int len, object value)
        {
            command.Parameters.Add(dbDriver.CreateParameter(name, dbType, len, value));
        }

        private void setValue<T>(string name, string dataType, Func<string, string, T, IDataParameter> getParameter, T value)
        {
            command.Parameters.Add(getParameter(name, dataType, value));
        }

        public void SetBool(string name, string dataType, bool? value)
        {
            setValue(name, dataType, dbDriver.CreateBoolParameter, value);
        }

        public void SetByte(string name, string dataType, byte? value)
        {
            setValue(name, dataType, dbDriver.CreateByteParameter, value);
        }

        public void SetChar(string name, string dataType, char? value)
        {
            setValue(name, dataType, dbDriver.CreateCharParameter, value);
        }

        public void SetShort(string name, string dataType, short? value)
        {
            setValue(name, dataType, dbDriver.CreateShortParameter, value);
        }

        public void SetInt(string name, string dataType, int? value)
        {
            setValue(name, dataType, dbDriver.CreateIntParameter, value);
        }

        public void SetLong(string name, string dataType, long? value)
        {
            setValue(name, dataType, dbDriver.CreateLongParameter, value);
        }

        public void SetFloat(string name, string dataType, float? value)
        {
            setValue(name, dataType, dbDriver.CreateFloatParameter, value);
        }

        public void SetDouble(string name, string dataType, double? value)
        {
            setValue(name, dataType, dbDriver.CreateDoubleParameter, value);
        }

        public void SetDecimal(string name, string dataType, decimal? value)
        {
            setValue(name, dataType, dbDriver.CreateDecimalParameter, value);
        }

        public void SetDateTime(string name, string dataType, DateTime? value)
        {
            setValue(name, dataType, dbDriver.CreateDateTimeParameter, value);
        }

        public void SetString(string name, string dataType, string value)
        {
            setValue(name, dataType, dbDriver.CreateStringParameter, value);
        }
    }
}
