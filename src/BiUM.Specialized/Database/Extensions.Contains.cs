using System;
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
        var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

        if (trNeedle is null || invariantNeedle is null)
        {
            return queryable;
        }

        var entityParam = Expression.Parameter(typeof(TSource), "e");
        var field = ReplaceParameter(selector.Body, selector.Parameters[0], entityParam);
        var contains = BuildContainsExpression(field, trNeedle, invariantNeedle);
        var lambda = Expression.Lambda<Func<TSource, bool>>(contains, entityParam);

        return queryable.Where(lambda);
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
