using System;
using System.Text.RegularExpressions;

namespace BiUM.Specialized.Services.DynamicApi;

public static partial class DynamicApiHandlerSourceNormalizer
{
    [GeneratedRegex(@"\bnew\s+ApiResponse\s*\{", RegexOptions.None)]
    private static partial Regex NonGenericApiResponsePattern();

    [GeneratedRegex(@"\bnew\s+(?:BiUM\.Contract\.Models\.Api\.)?PaginatedApiResponse\s*\(", RegexOptions.None)]
    private static partial Regex NonGenericPaginatedApiResponsePattern();

    public static string PrepareForCompile(string sourceCode, string? domainDbContextTypeFullName, bool usesDynamicTables)
    {
        var normalized = NormalizeHandlerResponses(sourceCode);

        if (!usesDynamicTables && !string.IsNullOrWhiteSpace(domainDbContextTypeFullName))
        {
            normalized = NormalizeDomainDbAccess(normalized);
        }

        return normalized;
    }

    public static string NormalizeHandlerResponses(string sourceCode) =>
        NormalizePaginatedApiResponse(NormalizeApiResponse(sourceCode));

    public static string NormalizeApiResponse(string sourceCode) =>
        NonGenericApiResponsePattern().Replace(sourceCode, "new ApiResponse<object> {");

    public static string NormalizePaginatedApiResponse(string sourceCode) =>
        NonGenericPaginatedApiResponsePattern().Replace(sourceCode, "new PaginatedApiResponse<object>(");

    public static string NormalizeDomainDbAccess(string sourceCode) =>
        sourceCode.Replace("ctx.Db", "__db", StringComparison.Ordinal);
}