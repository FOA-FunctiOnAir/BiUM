using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using System.Collections.Generic;

namespace System.Linq;

public static class Extensions
{
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
        var list = source as IList<BaseTranslationDto> ?? [.. source];

        return list.FirstOrDefault(x => x.LanguageId == Ids.Language.English.Id)?.Translation
            ?? list.FirstOrDefault()?.Translation
            ?? "";
    }
}