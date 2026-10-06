using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BiUM.Specialized.Database;

public static partial class Extensions
{
    private static readonly MethodInfo ApplyContainsEntityMethod = typeof(Extensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(ApplyContains) && m.GetParameters()[0].ParameterType.IsGenericParameter);

    private static readonly MethodInfo ApplyAnyContainsEntityMethod = typeof(Extensions)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == nameof(ApplyAnyContains) && m.GetParameters()[0].ParameterType.IsGenericParameter);

    public static bool ApplyContains<TSource>(
        this TSource entity,
        Expression<Func<TSource, string?>> selector,
        string? search)
    {
        var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

        if (trNeedle is null)
        {
            return true;
        }

        var value = (string?)EvaluateNode(selector.Body, entity) ?? string.Empty;

        return ApplyRule(value, TurkishRuleReplacements).Contains(trNeedle, StringComparison.Ordinal)
            || ApplyRule(value, InvariantRuleReplacements).Contains(invariantNeedle!, StringComparison.Ordinal);
    }

    public static bool ApplyAnyContains<TSource, TChild>(
        this TSource entity,
        Expression<Func<TSource, IEnumerable<TChild>>> collectionSelector,
        Expression<Func<TChild, string?>> childSelector,
        string? search,
        Expression<Func<TChild, bool>>? childFilter = null)
    {
        var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

        if (trNeedle is null)
        {
            return true;
        }

        var collection = (IEnumerable<TChild>?)EvaluateNode(collectionSelector.Body, entity);

        if (collection is null)
        {
            return false;
        }

        foreach (var child in collection)
        {
            if (child is null || (childFilter is not null && EvaluateNode(childFilter.Body, child) is not true))
            {
                continue;
            }

            var value = (string?)EvaluateNode(childSelector.Body, child);

            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (ApplyRule(value, TurkishRuleReplacements).Contains(trNeedle, StringComparison.Ordinal)
                || ApplyRule(value, InvariantRuleReplacements).Contains(invariantNeedle!, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Evaluates a selector/filter expression body directly against a concrete runtime value, without
    /// Expression.Compile() (no dynamic method / JIT cost). Covers the node shapes actually used by
    /// ApplyContains/ApplyAnyContains selectors across the codebase: member access chains (incl. the
    /// null-forgiving operator, which emits no extra node), ternary, string concatenation, and
    /// equality/inequality checks. Anything else throws NotSupportedException rather than guessing.
    /// This is the client-side (LINQ-to-Objects) fallback only - WhereExpanded never calls this, it
    /// rewrites the same selectors directly into the SQL-translated expression tree.
    /// </summary>
    private static object? EvaluateNode(Expression expression, object? root)
    {
        switch (expression)
        {
            case ParameterExpression:
                return root;

            case ConstantExpression constant:
                return constant.Value;

            case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary:
                return EvaluateNode(unary.Operand, root);

            case MemberExpression member:
                {
                    var target = member.Expression is null ? null : EvaluateNode(member.Expression, root);

                    if (member.Expression is not null && target is null)
                    {
                        return null;
                    }

                    return member.Member switch
                    {
                        PropertyInfo property => property.GetValue(target),
                        FieldInfo field => field.GetValue(target),
                        _ => throw new NotSupportedException($"ApplyContains fallback cannot evaluate member '{member.Member.Name}'.")
                    };
                }

            case ConditionalExpression conditional:
                return EvaluateNode(conditional.Test, root) is true
                    ? EvaluateNode(conditional.IfTrue, root)
                    : EvaluateNode(conditional.IfFalse, root);

            case BinaryExpression { NodeType: ExpressionType.Equal, Method: null } binary:
                return Equals(EvaluateNode(binary.Left, root), EvaluateNode(binary.Right, root));

            case BinaryExpression { NodeType: ExpressionType.NotEqual, Method: null } binary:
                return !Equals(EvaluateNode(binary.Left, root), EvaluateNode(binary.Right, root));

            case BinaryExpression { Method: not null } binary:
                return binary.Method.Invoke(null, [EvaluateNode(binary.Left, root), EvaluateNode(binary.Right, root)]);

            case MethodCallExpression call:
                {
                    var instance = call.Object is null ? null : EvaluateNode(call.Object, root);
                    var arguments = call.Arguments.Select(a => EvaluateNode(a, root)).ToArray();

                    return call.Method.Invoke(instance, arguments);
                }

            default:
                throw new NotSupportedException(
                    $"ApplyContains/ApplyAnyContains fallback cannot evaluate expression node '{expression.NodeType}'. " +
                    "Keep selectors to member access, ternary, string concatenation, or use WhereExpanded for queryable translation.");
        }
    }

    public static IQueryable<TSource> WhereExpanded<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate)
    {
        var visitor = new ContainsExpandVisitor();
        var expandedBody = visitor.Visit(predicate.Body);
        var expanded = Expression.Lambda<Func<TSource, bool>>(expandedBody, predicate.Parameters);

        return source.Where(expanded);
    }

    private static LambdaExpression UnwrapLambda(Expression expression) =>
        expression switch
        {
            UnaryExpression { NodeType: ExpressionType.Quote } unary => (LambdaExpression)unary.Operand,
            LambdaExpression lambda => lambda,
            _ => throw new InvalidOperationException(
                $"ApplyContains/ApplyAnyContains inside WhereExpanded require a literal lambda argument, found '{expression.NodeType}'.")
        };

    private static object? EvaluateExpression(Expression expression) =>
        expression is ConstantExpression constant
            ? constant.Value
            : Expression.Lambda(expression).Compile().DynamicInvoke();

    private sealed class ContainsExpandVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod)
            {
                var definition = node.Method.GetGenericMethodDefinition();

                if (definition == ApplyContainsEntityMethod)
                {
                    return ExpandApplyContains(node);
                }

                if (definition == ApplyAnyContainsEntityMethod)
                {
                    return ExpandApplyAnyContains(node);
                }
            }

            return base.VisitMethodCall(node);
        }

        private Expression ExpandApplyContains(MethodCallExpression node)
        {
            var entityExpression = Visit(node.Arguments[0]);
            var selector = UnwrapLambda(node.Arguments[1]);
            var search = (string?)EvaluateExpression(node.Arguments[2]);

            var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

            if (trNeedle is null)
            {
                return Expression.Constant(true);
            }

            var field = ReplaceParameter(selector.Body, selector.Parameters[0], entityExpression);

            return BuildContainsExpression(field, trNeedle, invariantNeedle!);
        }

        private Expression ExpandApplyAnyContains(MethodCallExpression node)
        {
            var entityExpression = Visit(node.Arguments[0]);
            var collectionSelector = UnwrapLambda(node.Arguments[1]);
            var childSelector = UnwrapLambda(node.Arguments[2]);
            var search = (string?)EvaluateExpression(node.Arguments[3]);
            var childFilterArgument = node.Arguments[4];
            var childFilter = childFilterArgument is ConstantExpression { Value: null }
                ? null
                : UnwrapLambda(childFilterArgument);

            var (trNeedle, invariantNeedle) = PrepareContainsNeedles(search);

            if (trNeedle is null)
            {
                return Expression.Constant(true);
            }

            var childType = childSelector.Parameters[0].Type;
            var collection = ReplaceParameter(collectionSelector.Body, collectionSelector.Parameters[0], entityExpression);
            var childParam = Expression.Parameter(childType, "c");
            var childField = ReplaceParameter(childSelector.Body, childSelector.Parameters[0], childParam);
            var notNull = Expression.NotEqual(childField, Expression.Constant(null, typeof(string)));
            var contains = BuildContainsExpression(childField, trNeedle, invariantNeedle!);
            Expression childBody = Expression.AndAlso(notNull, contains);

            if (childFilter is not null)
            {
                var filterBody = ReplaceParameter(childFilter.Body, childFilter.Parameters[0], childParam);
                childBody = Expression.AndAlso(filterBody, childBody);
            }

            var childPredicate = Expression.Lambda(childBody, childParam);

            return Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.Any),
                [childType],
                collection,
                childPredicate);
        }
    }
}