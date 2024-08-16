using System;
using System.Collections.Generic;

public static class LinqUtil
{
    // Using the answer by Jon Skeet from https://stackoverflow.com/questions/914109/how-to-use-linq-to-select-object-with-minimum-or-maximum-property-value

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, TSource valIfEmpty)
    {
        return MinBy(source, selector, Comparer<TKey>.Default, 1, valIfEmpty);
    }

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer, TSource valIfEmpty)
    {
        return MinBy(source, selector, comparer, 1, valIfEmpty);
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, TSource valIfEmpty)
    {
        return MinBy(source, selector, Comparer<TKey>.Default, -1, valIfEmpty);
    }

    public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer, TSource valIfEmpty)
    {
        return MinBy(source, selector, comparer, -1, valIfEmpty);
    }

    public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, IComparer<TKey> comparer, int sign, TSource valIfEmpty)
    {
        using (var sourceIterator = source.GetEnumerator())
        {
            if (!sourceIterator.MoveNext())
            {
                return valIfEmpty;
            }
            TSource min = sourceIterator.Current;
            TKey minKey = selector(min);
            while (sourceIterator.MoveNext())
            {
                var candidate = sourceIterator.Current;
                var candidateProjected = selector(candidate);
                if (null == minKey || sign * comparer.Compare(candidateProjected, minKey) < 0)
                {
                    min = candidate;
                    minKey = candidateProjected;
                }
            }
            return min;
        }
    }
}
