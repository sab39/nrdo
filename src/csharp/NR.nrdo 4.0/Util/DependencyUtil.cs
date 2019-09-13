using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NR.nrdo.Util
{
    public static class DependencyUtil
    {
        // This is a topological sort based on Tarjan's Algorithm as described on
        // https://en.wikipedia.org/wiki/Topological_sorting#Tarjan.27s_algorithm
        // Instead of adding to the head of the list (which is inefficient with an array-backed list like List<T>),
        // we append to the list and then reverse it afterwards.
        public static IEnumerable<T> DependencySort<T>(this IEnumerable<T> items, Func<T, T, bool> mustOccurBefore, IEqualityComparer<T> comparer = null)
        {
            if (comparer == null) comparer = EqualityComparer<T>.Default;

            var itemList = items.Distinct(comparer).ToList();
            var temporaryMarks = new HashSet<T>(comparer);
            var permanentMarks = new HashSet<T>(comparer);
            var result = new List<T>();

            while (permanentMarks.Count < itemList.Count)
            {
                dependencyVisit(itemList.First(s => !permanentMarks.Contains(s)), itemList, mustOccurBefore, comparer, temporaryMarks, permanentMarks, result);
            }

            result.Reverse();
            return result.AsReadOnly();
        }

        private static void dependencyVisit<T>(T item, List<T> itemList, Func<T, T, bool> mustOccurBefore, IEqualityComparer<T> comparer, HashSet<T> temporaryMarks, HashSet<T> permanentMarks, List<T> result)
        {
            if (temporaryMarks.Contains(item)) throw new ArgumentException("Can't resolve dependencies: circular dependency found (involving " + item + ")");

            if (!permanentMarks.Contains(item))
            {
                temporaryMarks.Add(item);

                foreach (var other in itemList)
                {
                    if (comparer.Equals(other, item)) continue;

                    if (mustOccurBefore(item, other))
                    {
                        dependencyVisit(other, itemList, mustOccurBefore, comparer, temporaryMarks, permanentMarks, result);
                    }
                }

                permanentMarks.Add(item);
                temporaryMarks.Remove(item);
                result.Add(item);
            }
        }
    }
}
