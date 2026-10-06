using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    private static readonly MethodInfo StringReplaceMethod =
        typeof(string).GetMethod(nameof(string.Replace), [typeof(string), typeof(string)])!;

    private static readonly (string From, string To)[] TurkishRuleReplacements =
    [
        ("i", "İ"),
        ("ı", "I"),
        ("Ş", "S"), ("ş", "S"),
        ("Ğ", "G"), ("ğ", "G"),
        ("Ü", "U"), ("ü", "U"),
        ("Ö", "O"), ("ö", "O"),
        ("Ç", "C"), ("ç", "C"),
    ];

    private static readonly (string From, string To)[] InvariantRuleReplacements =
    [
        ("i", "I"),
        ("Ş", "S"), ("ş", "S"),
        ("Ğ", "G"), ("ğ", "G"),
        ("Ü", "U"), ("ü", "U"),
        ("Ö", "O"), ("ö", "O"),
        ("Ç", "C"), ("ç", "C"),
    ];

    public static IQueryable<TSource> ApplyContains<TSource>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, string?>> selector,
        string? search)
    {
        if (PrepareContainsNeedles(search).Tr is null)
        {
            return queryable;
        }

        return queryable.Where(BuildContains(selector, search));
    }

    public static IQueryable<TSource> ApplyContains<TSource>(
        this IQueryable<TSource> queryable,
        string? search,
        params Expression<Func<TSource, string?>>[] selectors)
    {
        if (PrepareContainsNeedles(search).Tr is null || selectors.Length == 0)
        {
            return queryable;
        }

        Expression<Func<TSource, bool>>? combined = null;

        foreach (var selector in selectors)
        {
            var predicate = BuildContains(selector, search);
            combined = combined is null ? predicate : combined.Or(predicate);
        }

        return queryable.Where(combined!);
    }

    public static IQueryable<TSource> ApplyAnyContains<TSource, TChild>(
        this IQueryable<TSource> queryable,
        Expression<Func<TSource, IEnumerable<TChild>>> collectionSelector,
        Expression<Func<TChild, string?>> childSelector,
        string? search,
        Expression<Func<TChild, bool>>? childFilter = null)
    {
        if (PrepareContainsNeedles(search).Tr is null)
        {
            return queryable;
        }

        return queryable.Where(BuildAnyContains(collectionSelector, childSelector, search, childFilter));
    }

    public static Expression<Func<TSource, bool>> BuildContains<TSource>(
        Expression<Func<TSource, string?>> selector,
        string? search)
    {
        var entityParam = Expression.Parameter(typeof(TSource), "e");
        var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

        if (trNeedle is null)
        {
            return Expression.Lambda<Func<TSource, bool>>(Expression.Constant(true), entityParam);
        }

        var field = ReplaceParameter(selector.Body, selector.Parameters[0], entityParam);
        var contains = BuildContainsExpression(field, trNeedle, invariantNeedle!);

        return Expression.Lambda<Func<TSource, bool>>(contains, entityParam);
    }

    public static Expression<Func<TSource, bool>> BuildAnyContains<TSource, TChild>(
        Expression<Func<TSource, IEnumerable<TChild>>> collectionSelector,
        Expression<Func<TChild, string?>> childSelector,
        string? search,
        Expression<Func<TChild, bool>>? childFilter = null)
    {
        var entityParam = Expression.Parameter(typeof(TSource), "e");
        var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

        if (trNeedle is null)
        {
            return Expression.Lambda<Func<TSource, bool>>(Expression.Constant(true), entityParam);
        }

        var collection = ReplaceParameter(collectionSelector.Body, collectionSelector.Parameters[0], entityParam);
        var childParam = Expression.Parameter(typeof(TChild), "c");
        var childField = ReplaceParameter(childSelector.Body, childSelector.Parameters[0], childParam);
        var notNull = Expression.NotEqual(childField, Expression.Constant(null, typeof(string)));
        var contains = BuildContainsExpression(childField, trNeedle, invariantNeedle!);
        Expression childBody = Expression.AndAlso(notNull, contains);

        if (childFilter is not null)
        {
            var filterBody = ReplaceParameter(childFilter.Body, childFilter.Parameters[0], childParam);
            childBody = Expression.AndAlso(filterBody, childBody);
        }

        var childPredicate = Expression.Lambda<Func<TChild, bool>>(childBody, childParam);

        var anyCall = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Any),
            [typeof(TChild)],
            collection,
            childPredicate);

        return Expression.Lambda<Func<TSource, bool>>(anyCall, entityParam);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var param = Expression.Parameter(typeof(T), "x");
        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], param);
        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], param);

        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(leftBody, rightBody), param);
    }

    private static (string? Tr, string? Invariant) PrepareContainsNeedles(string? search)
    {
        var trimmed = (search ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            return (null, null);
        }

        return (ApplyRule(trimmed, TurkishRuleReplacements), ApplyRule(trimmed, InvariantRuleReplacements));
    }

    private static string ApplyRule(string text, (string From, string To)[] replacements)
    {
        foreach (var (from, to) in replacements)
        {
            text = text.Replace(from, to);
        }

        return text.ToUpperInvariant();
    }

    private static Expression BuildContainsExpression(Expression stringExpression, string trNeedle, string invariantNeedle)
    {
        var coalesced = Expression.Coalesce(stringExpression, Expression.Constant(string.Empty));

        var trContains = Expression.Call(
            ApplyRuleExpression(coalesced, TurkishRuleReplacements),
            nameof(string.Contains),
            Type.EmptyTypes,
            Expression.Constant(trNeedle));

        var invariantContains = Expression.Call(
            ApplyRuleExpression(coalesced, InvariantRuleReplacements),
            nameof(string.Contains),
            Type.EmptyTypes,
            Expression.Constant(invariantNeedle));

        return Expression.OrElse(trContains, invariantContains);
    }

    private static Expression ApplyRuleExpression(Expression stringExpression, (string From, string To)[] replacements)
    {
        Expression current = stringExpression;

        foreach (var (from, to) in replacements)
        {
            current = Expression.Call(current, StringReplaceMethod, Expression.Constant(from), Expression.Constant(to));
        }

        return Expression.Call(current, nameof(string.ToUpper), Type.EmptyTypes);
    }

    private sealed class ContainsParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression source, ParameterExpression target) =>
        new ContainsParameterReplacer(source, target).Visit(expression)!;
}
