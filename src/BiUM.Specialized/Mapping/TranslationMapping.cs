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

    public static Expression<Func<TEntity, IList<TDto>?>> GetColumnTranslationsExpr<TEntity, TTranslation, TDto>(
        Expression<Func<TEntity, IEnumerable<TTranslation>>> navigationSelector,
        string columnName)
        where TTranslation : TranslationBaseEntity
        where TDto : BaseTranslationDto, new()
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

        // Project straight into TDto inside the expression tree instead of relying on AutoMapper's
        // ProjectTo to nested-map a materialized IList<TTranslation> afterwards — that nested mapping
        // is never applied (the member silently ends up null), because the source expression already
        // resolves to a concrete entity-typed list by the time ProjectTo's visitor sees it.
        var dtoIdProperty = typeof(TDto).GetProperty(nameof(BaseTranslationDto.Id))!;
        var dtoLanguageIdProperty = typeof(TDto).GetProperty(nameof(BaseTranslationDto.LanguageId))!;
        var dtoTranslationProperty = typeof(TDto).GetProperty(nameof(BaseTranslationDto.Translation))!;

        var projectionLambda = Expression.Lambda<Func<TTranslation, TDto>>(
            Expression.MemberInit(
                Expression.New(typeof(TDto)),
                Expression.Bind(dtoIdProperty, Expression.Property(tParam, nameof(TranslationBaseEntity.Id))),
                Expression.Bind(dtoLanguageIdProperty, Expression.Property(tParam, nameof(TranslationBaseEntity.LanguageId))),
                Expression.Bind(dtoTranslationProperty, Expression.Property(tParam, nameof(TranslationBaseEntity.Translation)))),
            tParam);

        var selectCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Select), [typeof(TTranslation), typeof(TDto)],
            whereCall,
            projectionLambda);

        var toListCall = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.ToList), [typeof(TDto)],
            selectCall);

        return Expression.Lambda<Func<TEntity, IList<TDto>?>>(
            Expression.Convert(toListCall, typeof(IList<TDto>)),
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