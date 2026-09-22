using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BiUM.Specialized.Mapping;

public static class TranslationMapping
{
    public static Expression<Func<TEntity, string?>> GetColumnTranslationExpr<TEntity, TTranslation>(
        Expression<Func<TEntity, IEnumerable<TTranslation>>> navigationSelector,
        string columnName)
        where TTranslation : TranslationBaseEntity
    {
        Guid languageId = default;
        Expression<Func<TTranslation, bool>> filter =
            t => (languageId == default || t.LanguageId == languageId) && t.Column == columnName;

        var entityParam = navigationSelector.Parameters[0];
        var navBody = navigationSelector.Body;
        var tParam = filter.Parameters[0];

        var whereCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Where), [typeof(TTranslation)],
            navBody,
            filter);

        var selectCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Select), [typeof(TTranslation), typeof(string)],
            whereCall,
            Expression.Lambda<Func<TTranslation, string>>(
                Expression.Property(tParam, nameof(TranslationBaseEntity.Translation)),
                tParam));

        var firstCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.FirstOrDefault), [typeof(string)],
            selectCall);

        return Expression.Lambda<Func<TEntity, string?>>(firstCall, entityParam);
    }

    public static Expression<Func<TEntity, IList<TTranslation>?>> GetColumnTranslationsExpr<TEntity, TTranslation>(
        Expression<Func<TEntity, IEnumerable<TTranslation>>> navigationSelector,
        string columnName)
        where TTranslation : TranslationBaseEntity
    {
        var entityParam = navigationSelector.Parameters[0];
        var navBody = navigationSelector.Body;
        var tParam = Expression.Parameter(typeof(TTranslation), "t");

        var whereCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Where), [typeof(TTranslation)],
            navBody,
            Expression.Lambda<Func<TTranslation, bool>>(
                Expression.Equal(
                    Expression.Property(tParam, nameof(TranslationBaseEntity.Column)),
                    Expression.Constant(columnName)),
                tParam));

        var toListCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.ToList), [typeof(TTranslation)],
            whereCall);

        return Expression.Lambda<Func<TEntity, IList<TTranslation>?>>(
            Expression.Convert(toListCall, typeof(IList<TTranslation>)),
            entityParam);
    }

    public static string ToTranslationString(this IEnumerable<BaseTranslationDto> source)
    {
        var list = source as IList<BaseTranslationDto> ?? [.. source];

        return list.FirstOrDefault(x => x.LanguageId == Ids.Language.English.Id)?.Translation
            ?? list.FirstOrDefault()?.Translation
            ?? "";
    }
}