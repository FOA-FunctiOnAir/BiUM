using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BiUM.Specialized.Services.DynamicApi;

public static class DynamicApiEntityParser
{
    public sealed class ParseResult
    {
        public IReadOnlyList<DynamicApiTableReference> TableReferences { get; init; } = [];
        public IReadOnlyList<string> Errors { get; init; } = [];
    }

    public static ParseResult Parse(string sourceCode)
    {
        var references = new List<DynamicApiTableReference>();
        var errors = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return new ParseResult();
        }

        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            if (!string.Equals(memberAccess.Name.Identifier.Text, "Entity", StringComparison.Ordinal))
            {
                continue;
            }

            if (memberAccess.Expression is not IdentifierNameSyntax { Identifier.Text: "ctx" })
            {
                errors.Add("dynamic_api_entity_literal_required");
                continue;
            }

            var args = invocation.ArgumentList.Arguments;

            if (args.Count != 2)
            {
                errors.Add("dynamic_api_entity_literal_required");
                continue;
            }

            if (!TryGetStringLiteral(args[0].Expression, out var schema)
                || !TryGetStringLiteral(args[1].Expression, out var table))
            {
                errors.Add("dynamic_api_entity_literal_required");
                continue;
            }

            var reference = new DynamicApiTableReference(schema, table);

            if (seen.Add(reference.Key))
            {
                references.Add(reference);
            }
        }

        return new ParseResult
        {
            TableReferences = references,
            Errors = errors.Distinct().ToList()
        };
    }

    private static bool TryGetStringLiteral(ExpressionSyntax expression, out string value)
    {
        value = string.Empty;

        if (expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression)
            && literal.Token.Value is string s)
        {
            value = s;
            return true;
        }

        return false;
    }
}