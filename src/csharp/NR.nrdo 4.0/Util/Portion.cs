using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Util
{
    /// <summary>
    /// Mini rational number class that provides quick access to its value as a percentage, and ToString()s to percentage by default.
    /// </summary>
    public struct Portion : IComparable<Portion>
    {
        private readonly long storedNumerator;
        private readonly long storedDenominator;

        /// We follow the convention that default(T) on numeric types represents 0 by treating a raw denominator if 0 as if it were 1.
        public long Numerator { get { return storedNumerator; } }
        public long Denominator { get { return storedDenominator == 0 ? 1 : storedDenominator; } }

        /// Because this class is used for representing progress, explicitly passing 0,0 to the constructor is treated as the scenario where
        /// there's no work TO do, and therefore it's already complete, even when no work HAS been done.
        /// This gets treated as special value ZeroOfZero, which is stored as 1/0 to distinguish it from default(Percentage).
        /// ZeroOfZero is treated as 100% for most purposes (Numerator/Denominator say it's 1/1), except that addition treats it as zero,
        /// and equality comparisons don't treat it as equal to a "real" 100% value.
        /// The RawNumerator/RawDenominator properties can be used when the 0/0 meaning is desired for ZeroOfZero
        /// RawNumerator can be used with the regular Denominator property to treat ZeroOfZero as zero.
        private bool isZeroOfZero { get { return storedDenominator == 0 && storedNumerator == 1; } }

        public long RawNumerator { get { return isZeroOfZero ? 0 : Numerator; } }
        public long RawDenominator { get { return isZeroOfZero ? 0 : Denominator; } }

        public static Portion Zero { get { return default(Portion); } }
        public static Portion Complete { get { return new Portion(1, 1); } }

        public static Portion ZeroOfZero { get { return new Portion(0, 0); } }

        public decimal AsPercentage { get { return Numerator * 100m / Denominator; } }

        public static Portion Percentage(int percent)
        {
            return Ratio(percent, 100);
        }

        public static Portion Ratio(long numerator, long denominator)
        {
            return new Portion(numerator, denominator);
        }
        public static Portion Ratio(TimeSpan numerator, TimeSpan denominator)
        {
            return Ratio(numerator.Ticks, denominator.Ticks);
        }
        public static Portion SafeRatio(long numerator, long denominator)
        {
            return denominator == 0 ? ZeroOfZero : Ratio(numerator, denominator);
        }
        public static Portion SafeRatio(TimeSpan numerator, TimeSpan denominator)
        {
            return SafeRatio(numerator.Ticks, denominator.Ticks);
        }

        public static Portion Max(Portion a, Portion b)
        {
            if (a == ZeroOfZero) return b;
            if (b == ZeroOfZero) return a;
            return a < b ? a : b;
        }
        public static Portion Min(Portion a, Portion b)
        {
            if (a == ZeroOfZero) return b;
            if (b == ZeroOfZero) return a;
            return a < b ? b : a;
        }

        private Portion(long numerator, long denominator)
        {
            if (denominator == 0)
            {
                if (numerator != 0) throw new DivideByZeroException();
                this.storedNumerator = 1;
                this.storedDenominator = 0;
                return;
            }

            if (numerator == 0)
            {
                this.storedNumerator = 0;
                this.storedDenominator = 1;
                return;
            }

            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }
            var gcd = getGcd(numerator, denominator);
            this.storedNumerator = numerator / gcd;
            this.storedDenominator = denominator / gcd;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Portion)) return false;
            return this == (Portion)obj;
        }

        private static long getGcd(long a, long b)
        {
            return b == 0 ? a : getGcd(b, a % b);
        }

        public override int GetHashCode()
        {
            if (isZeroOfZero) return (-1L).GetHashCode();
            if (storedNumerator == 0) return (0L).GetHashCode();

            var gcd = getGcd(Denominator, Numerator);
            return (Numerator / gcd).GetHashCode() + 7 * (Denominator / gcd).GetHashCode();
        }

        public int CompareTo(Portion other)
        {
            return (this.Numerator * other.Denominator).CompareTo(other.Numerator * this.Denominator);
        }

        public override string ToString()
        {
            return AsPercentage.ToString("#0'%'");
        }

        public static bool operator ==(Portion a, Portion b)
        {
            // If the stored values are identical then the values are equal. Testing this explicitly ensures ZeroOfZero equals itself, and
            // shortcuts doing multiplications when they're not needed.
            if (a.storedNumerator == b.storedNumerator && a.storedDenominator == b.storedDenominator) return true;

            // If the numerators are both zero then the values are equal regardless of denominator.
            if (a.storedNumerator == 0 && b.storedNumerator == 0) return true;

            // If storedDenominator is zero then we've got one of two special cases:
            // - if storedNumerator is 1 then the value is ZeroOfZero
            // - if storedNumerator is 0 then the value is zero
            // Both of these were covered by the checks above, so if we reached this point with either of the storedDenominators being zero the values aren't equal.
            if (a.storedDenominator == 0 || b.storedDenominator == 0) return false;

            // Now we know both storedDenominators are nonzero, so storedNumerator/storedDenominator are interchangeable with Numerator/Denominator
            return a.storedNumerator * b.storedDenominator == b.storedNumerator * a.storedDenominator;
        }

        public static bool operator !=(Portion a, Portion b)
        {
            return !(a == b);
        }

        public static bool operator >(Portion a, Portion b)
        {
            return a.CompareTo(b) > 0;
        }
        public static bool operator >=(Portion a, Portion b)
        {
            return a.CompareTo(b) >= 0;
        }
        public static bool operator <(Portion a, Portion b)
        {
            return a.CompareTo(b) < 0;
        }
        public static bool operator <=(Portion a, Portion b)
        {
            return a.CompareTo(b) <= 0;
        }
        public static Portion operator -(Portion a)
        {
            // Using RawNumerator/RawDenominator ensures that -ZeroOfZero gives ZeroOfZero
            return Ratio(-a.RawNumerator, a.RawDenominator);
        }
        public static Portion operator +(Portion a, Portion b)
        {
            // These operators use RawNumerator with non-Raw denominator so ZeroOfZero is treated as 0/1, ie zero
            return Ratio(a.RawNumerator * b.Denominator + b.RawNumerator * a.Denominator, a.Denominator * b.Denominator);
        }
        public static Portion operator -(Portion a, Portion b)
        {
            return a + (-b);
        }
        public static Portion operator *(Portion a, Portion b)
        {
            return Ratio(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        }
        public static Portion operator /(Portion a, Portion b)
        {
            return Ratio(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        }

        public static implicit operator Portion(long num)
        {
            return Ratio(num, 1);
        }
    }
}
