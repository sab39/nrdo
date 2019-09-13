using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Schema.Shared
{
    internal static class Helpers
    {
        internal static IEnumerable<T> EnumerableFrom<T>(T t)
        {
            yield return t;
        }
        internal static IEnumerable<T> EnumerableFrom<T>(params T[] ts)
        {
            return ts;
        }

        internal static KeyValuePair<K, V> KeyValuePair<K, V>(K key, V value)
        {
            return new KeyValuePair<K, V>(key, value);
        }

        internal static string ToSql(this TriggerTiming timing)
        {
            switch (timing) {
                case TriggerTiming.After: return "AFTER";
                case TriggerTiming.Before: return "BEFORE";
                case TriggerTiming.InsteadOf: return "INSTEAD OF";
                default: throw new ArgumentException("Invalid trigger timing");
            }
        }

        private static IEnumerable<string> eachEvent(TriggerEvents events)
        {
            if (events.HasFlag(TriggerEvents.Insert)) yield return "INSERT";
            if (events.HasFlag(TriggerEvents.Update)) yield return "UPDATE";
            if (events.HasFlag(TriggerEvents.Delete)) yield return "DELETE";
        }
        internal static string ToSql(this TriggerEvents events)
        {
            return string.Join(", ", eachEvent(events));
        }
    }
}
