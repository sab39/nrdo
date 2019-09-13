using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using NR.nrdo.Connection;

namespace NR.nrdo.Internal
{
    public static class DbDataTypes
    {
        // The unsigned types and the signed byte type (uint, ushort, ulong, sbyte) do not have corresponding
        // get methods in IDataReader so they cannot easily be supported. All except UInt64 could be handled
        // by downcasting from the next signed size up, but that is extra effort for something that's never even
        // been attempted to be used.

        // bool -> Boolean
        public static void SetBoolParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, bool? value)
            where T : DBObject<T>
        {
            cmd.SetBool(name, dbType, value);
        }

        public static bool? ReadBoolValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetBool(index);
        }
        
        // byte -> Byte
        public static void SetByteParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, byte? value)
            where T : DBObject<T>
        {
            cmd.SetByte(name, dbType, value);
        }

        public static byte? ReadByteValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetByte(index);
        }

        // char -> dbtype StringFixedLength, IDataType Char
        public static void SetCharParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, char value)
            where T : DBObject<T>
        {
            cmd.SetChar(name, dbType, value);
        }

        public static char? ReadCharValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetChar(index);
        }

        // short -> Int16
        public static void SetShortParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, short? value)
            where T : DBObject<T>
        {
            cmd.SetShort(name, dbType, value);
        }

        public static short? ReadShortValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetShort(index);
        }

        // int -> Int32
        public static void SetIntParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, int? value)
            where T : DBObject<T>
        {
            cmd.SetInt(name, dbType, value);
        }

        public static int? ReadIntValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetInt(index);
        }

        // long -> Int64
        public static void SetLongParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, long? value)
            where T : DBObject<T>
        {
            cmd.SetLong(name, dbType, value);
        }

        public static long? ReadLongValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetLong(index);
        }

        // float -> dbtype Single, IDataType Float
        public static void SetFloatParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, float? value)
            where T : DBObject<T>
        {
            cmd.SetFloat(name, dbType, value);
        }

        public static float? ReadFloatValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetFloat(index);
        }

        // double -> Double
        public static void SetDoubleParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, double? value)
            where T : DBObject<T>
        {
            cmd.SetDouble(name, dbType, value);
        }

        public static double? ReadDoubleValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetDouble(index);
        }

        // string -> String
        public static void SetStringParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, string value)
            where T : DBObject<T>
        {
            cmd.SetString(name, dbType, value);
        }

        public static string ReadStringValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetString(index);
        }

        // decimal -> dbtype (if sqltype = $dbmoneytype$ then Currency else Decimal), IDataType Decimal
        public static void SetDecimalParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, decimal? value)
            where T : DBObject<T>
        {
            cmd.SetDecimal(name, dbType, value);
        }

        public static decimal? ReadDecimalValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetDecimal(index);
        }

        // DateTime -> DateTime
        public static void SetDateTimeParameter<T>(this T dbobject, NrdoCommand cmd, string name, string dbType, DateTime? value)
            where T : DBObject<T>
        {
            cmd.SetDateTime(name, dbType, value);
        }

        public static DateTime? ReadDateTimeValue<T>(this T dbobject, string name, NrdoResult result, int index)
            where T : DBObject<T>
        {
            return result.GetDateTime(index);
        }
    }
}
