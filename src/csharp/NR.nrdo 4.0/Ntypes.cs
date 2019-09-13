using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.SqlParser.Metadata;

namespace NR.nrdo
{
    public static class Nint
    {
        public static int Len(int? n)
        {
            return 4;
        }
        public static int? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return int.Parse(n);
        }
        public static IEqualityComparer<int?> DBEquivalentComparer { get { return EqualityComparer<int?>.Default; } }
    }
    public static class Nstring
    {
        public static int Len(string n)
        {
            int len = n == null ? 1 : n.Length;
            return len;
            // This apparently causes problems on cmSetup. Not sure why, as yet.
            //if (len > 65535) return len;
            //int result = 32;
            //while (len > result) result <<= 1;
            //return result;
        }
        public static string Parse(string n)
        {
            return (n == string.Empty) ? null : n;
        }

        [Serializable]
        private class Comparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return CollationInfo.Default.EqualityComparer.Equals(x, y);
            }

            public int GetHashCode(string obj)
            {
                return CollationInfo.Default.EqualityComparer.GetHashCode(obj);
            }
        }
        private static Comparer comparer = new Comparer();

        public static IEqualityComparer<string> DBEquivalentComparer { get { return comparer; } }
    }
    public static class Nbool
    {
        public static int Len(bool? n)
        {
            return 1;
        }
        public static bool? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return bool.Parse(n);
        }
        public static IEqualityComparer<bool?> DBEquivalentComparer { get { return EqualityComparer<bool?>.Default; } }
    }
    public static class Nbyte
    {
        public static int Len(byte? n)
        {
            return 1;
        }
        public static byte? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return byte.Parse(n);
        }
        public static IEqualityComparer<byte?> DBEquivalentComparer { get { return EqualityComparer<byte?>.Default; } }
    }
    public static class Nchar
    {
        public static int Len(char? n)
        {
            return 1;
        }
        public static char? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else if (n.Length > 1) throw new InvalidCastException(); // FIXME
            else return n[0];
        }
        public static IEqualityComparer<char?> DBEquivalentComparer { get { return EqualityComparer<char?>.Default; } }
    }
    public static class NDateTime
    {
        public static int Len(DateTime? n)
        {
            return 8;
        }
        public static DateTime? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return DateTime.Parse(n);
        }
        public static IEqualityComparer<DateTime?> DBEquivalentComparer { get { return EqualityComparer<DateTime?>.Default; } }
    }
    public static class Ndecimal
    {
        public static int Len(decimal? n)
        {
            return 9;
        }
        public static decimal? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return decimal.Parse(n);
        }
        public static IEqualityComparer<decimal?> DBEquivalentComparer { get { return EqualityComparer<decimal?>.Default; } }
    }
    public static class Ndouble
    {
        public static int Len(double? n)
        {
            return 8;
        }
        public static double? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return double.Parse(n);
        }
        public static IEqualityComparer<double?> DBEquivalentComparer { get { return EqualityComparer<double?>.Default; } }
    }
    public static class Nfloat
    {
        public static int Len(float? n)
        {
            return 4;
        }
        public static float? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return float.Parse(n);
        }
        public static IEqualityComparer<float?> DBEquivalentComparer { get { return EqualityComparer<float?>.Default; } }
    }
    public static class Nlong
    {
        public static int Len(long? n)
        {
            return 8;
        }
        public static long? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return long.Parse(n);
        }
        public static IEqualityComparer<long?> DBEquivalentComparer { get { return EqualityComparer<long?>.Default; } }
    }
    public static class Nsbyte
    {
        public static int Len(sbyte? n)
        {
            return 1;
        }
        public static sbyte? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return sbyte.Parse(n);
        }
        public static IEqualityComparer<sbyte?> DBEquivalentComparer { get { return EqualityComparer<sbyte?>.Default; } }
    }
    public static class Nshort
    {
        public static int Len(short? n)
        {
            return 2;
        }
        public static short? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return short.Parse(n);
        }
        public static IEqualityComparer<short?> DBEquivalentComparer { get { return EqualityComparer<short?>.Default; } }
    }
    public static class Nuint
    {
        public static int Len(uint? n)
        {
            return 4;
        }
        public static uint? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return uint.Parse(n);
        }
        public static IEqualityComparer<uint?> DBEquivalentComparer { get { return EqualityComparer<uint?>.Default; } }
    }
    public static class Nulong
    {
        public static int Len(ulong? n)
        {
            return 8;
        }
        public static ulong? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return ulong.Parse(n);
        }
        public static IEqualityComparer<ulong?> DBEquivalentComparer { get { return EqualityComparer<ulong?>.Default; } }
    }
    public static class Nushort
    {
        public static int Len(ushort? n)
        {
            return 2;
        }
        public static ushort? Parse(string n)
        {
            if (n == null || n == string.Empty) return null;
            else return ushort.Parse(n);
        }
        public static IEqualityComparer<ushort?> DBEquivalentComparer { get { return EqualityComparer<ushort?>.Default; } }
    }
}
