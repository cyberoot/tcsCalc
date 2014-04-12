using System;
using System.Collections.Generic;
using System.Linq;

namespace tcsCalc.Core.Util
{
    internal static class EnumerableExtension
    {
        public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0 ? new[] { new T[0] } :
              elements.SelectMany((e, i) =>
                elements.Skip(i + 1).Combinations(k - 1).Select(c => (new[] { e }).Concat(c)));
        }

        public static IEnumerable<IEnumerable<T>> Permute<T>(this IEnumerable<T> list, int k, bool withRepeat = false)
        {
            if (list.Count() == 1)
                return new List<IEnumerable<T>> { list };
            return
                k == 0 ? new[] { new T[0] } :
                list
                .Select((a, i1) =>
                            Permute(list.Where((b, i2) => withRepeat || (i2 != i1)), k - 1, withRepeat)
                           .Select(b => (new List<T> { a }).Concat(b)))
                .SelectMany(c => c);
        }

        public static void ModifyEach<T>(this IList<T> source, Func<T, T> projection)
        {
            for (int i = 0; i < source.Count; i++)
            {
                source[i] = projection(source[i]);
            }
        }
    }
}
