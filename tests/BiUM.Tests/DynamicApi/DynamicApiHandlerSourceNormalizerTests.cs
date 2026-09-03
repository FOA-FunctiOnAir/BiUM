using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiHandlerSourceNormalizerTests
{
    [Fact]
    public void NormalizeApiResponse_rewrites_non_generic_constructor()
    {
        const string source = "return new ApiResponse { Value = items };";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizeApiResponse(source);

        normalized.Should().Be("return new ApiResponse<object> { Value = items };");
    }

    [Fact]
    public void NormalizeApiResponse_leaves_generic_constructor_unchanged()
    {
        const string source = "return new ApiResponse<int> { Value = 1 };";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizeApiResponse(source);

        normalized.Should().Be(source);
    }

    [Fact]
    public void NormalizePaginatedApiResponse_rewrites_non_generic_constructor()
    {
        const string source = "return new PaginatedApiResponse(items, 2, 1, 10);";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizePaginatedApiResponse(source);

        normalized.Should().Be("return new PaginatedApiResponse<object>(items, 2, 1, 10);");
    }

    [Fact]
    public void NormalizePaginatedApiResponse_rewrites_fully_qualified_non_generic_constructor()
    {
        const string source = "return new BiUM.Contract.Models.Api.PaginatedApiResponse(items, 2, 1, 10);";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizePaginatedApiResponse(source);

        normalized.Should().Be("return new PaginatedApiResponse<object>(items, 2, 1, 10);");
    }

    [Fact]
    public void NormalizePaginatedApiResponse_leaves_generic_constructor_unchanged()
    {
        const string source = "return new PaginatedApiResponse<string>(items, 2, 1, 10);";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizePaginatedApiResponse(source);

        normalized.Should().Be(source);
    }

    [Fact]
    public void NormalizeDomainDbAccess_replaces_ctx_Db_with___db()
    {
        const string source = "var count = await ctx.Db.DomainDynamicApis.CountAsync(cancellationToken);";

        var normalized = DynamicApiHandlerSourceNormalizer.NormalizeDomainDbAccess(source);

        normalized.Should().Be("var count = await __db.DomainDynamicApis.CountAsync(cancellationToken);");
    }

    [Fact]
    public void PrepareForCompile_applies_both_normalizations_for_domain_db_path()
    {
        const string source = """
            var items = await ctx.Db.Currencies.ToListAsync(cancellationToken);
            return new ApiResponse { Value = items };
            """;

        var normalized = DynamicApiHandlerSourceNormalizer.PrepareForCompile(
            source,
            "BiApp.Test.Infrastructure.Persistence.TestDbContext",
            usesDynamicTables: false);

        normalized.Should().Contain("__db.Currencies");
        normalized.Should().Contain("new ApiResponse<object> { Value = items }");
        normalized.Should().NotContain("ctx.Db");
    }
}