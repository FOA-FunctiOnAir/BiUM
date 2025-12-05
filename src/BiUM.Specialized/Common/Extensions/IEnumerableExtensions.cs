using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;

namespace System.Linq;

public static class Extensions
{
    public static List<Guid> ToGuidList(this IEnumerable<string> source)
    {
        var guidArray = new List<Guid>();

        foreach (var s in source)
        {
            if (Guid.TryParse(s, out var g))
            {
                guidArray.Add(g);
            }
        }

        return guidArray;
    }

    public static string[] ToStringList(this IEnumerable<Guid> source)
    {
        var stringArray = source.Select(x => x.ToString()).ToArray();

        return stringArray;
    }

    public static TSource[] ToArray<TSource>(this TSource source)
    {
        if (source is null || source.GetType().IsArray)
        {
            return [];
        }

        return [source];
    }

    public static TResult[] ToArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        var stringArray = source.Select(selector).ToArray();

        return stringArray;
    }

    public static string? GetColumnTranslation<TSource>(this IEnumerable<TSource> source, string columnName)
        where TSource : TranslationBaseEntity
    {
        return source.FirstOrDefault(x => x.Column.Equals(columnName))?.Translation;
    }

    public static IList<TSource>? GetColumnTranslations<TSource>(this IEnumerable<TSource> source, string columnName)
        where TSource : TranslationBaseEntity
    {
        return source.Where(x => x.Column.Equals(columnName)).ToList();
    }

    public static string ToTranslationString(this IEnumerable<BaseTranslationDto> source)
    {
        return source.FirstOrDefault()?.Translation ?? "";
    }
}