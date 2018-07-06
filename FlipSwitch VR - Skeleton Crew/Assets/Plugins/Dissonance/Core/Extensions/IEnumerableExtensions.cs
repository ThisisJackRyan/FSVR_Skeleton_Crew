using System.Collections.Generic;

namespace Dissonance.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T tail)
        {
            foreach (var item in enumerable)
                yield return item;
            yield return tail;
        }
    }
}
