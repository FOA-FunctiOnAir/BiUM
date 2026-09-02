using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Services.DynamicApi;

public static class DynamicApiSourceTransformer
{
    public static string Transform(string sourceCode, IReadOnlyList<DynamicApiTableReference> references)
    {
        var transformed = sourceCode;

        foreach (var reference in references)
        {
            var entityType = DynamicApiEntityCodegen.ToEntityTypeName(reference.Schema, reference.TableName);
            var pattern = $"ctx.Entity(\"{EscapeRegex(reference.Schema)}\", \"{EscapeRegex(reference.TableName)}\")";
            var replacement = $"__dynamicTables.Set<{entityType}>()";
            transformed = transformed.Replace(pattern, replacement, StringComparison.Ordinal);
        }

        return transformed;
    }

    private static string EscapeRegex(string value) => value;
}