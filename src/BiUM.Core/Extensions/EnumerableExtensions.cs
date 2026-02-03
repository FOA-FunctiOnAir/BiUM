using System.Collections.Generic;

namespace System.Linq;

public static class Extensions
{
    public static IEnumerable<Guid> ToGuidEnumerable(this IEnumerable<string> source)
    {
        foreach (var s in source)
        {
            if (Guid.TryParse(s, out var g))
            {
                yield return g;
            }
        }
    }

    public static IEnumerable<string> ToStringEnumerable(this IEnumerable<Guid> source, string? format = null, IFormatProvider? provider = null)
    {
        foreach (var guid in source)
        {
            yield return guid.ToString(format, provider);
        }
    }

    public static IEnumerable<TSource> WrapWithEnumerable<TSource>(this TSource source)
    {
        yield return source;
    }

    public static TSource[] WrapWithArray<TSource>(this TSource source)
    {
        if (source is null)
        {
            return [];
        }

        return [source];
    }

    public static List<TSource> WrapWithList<TSource>(this TSource source)
    {
        if (source is null)
        {
            return [];
        }

        return [source];
    }

    public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        return source.Select(selector).ToArray();
    }
}