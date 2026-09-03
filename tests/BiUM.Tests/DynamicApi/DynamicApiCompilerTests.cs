using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiCompilerTests
{
    [Fact]
    public void Compile_succeeds_for_valid_handler_body()
    {
        var result = DynamicApiCompiler.Compile(
            "return new BiUM.Contract.Models.Api.ApiResponse();",
            "sample-code");

        result.Success.Should().BeTrue();
        result.AssemblyBytes.Should().NotBeNullOrEmpty();
        result.EntryPointTypeName.Should().StartWith("BiUM.DynamicApi.Generated.Handler_");
    }

    [Fact]
    public void Compile_fails_for_invalid_handler_body()
    {
        var result = DynamicApiCompiler.Compile("this is not valid csharp", "bad-code");

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Compile_succeeds_for_ef_linq_handler_body()
    {
        const string source = """
            var count = await ctx.Db.DomainDynamicApis.CountAsync(cancellationToken);
            return new BiUM.Contract.Models.Api.ApiResponse<int> { Value = count };
            """;

        var result = DynamicApiCompiler.Compile(source, "ef-linq");

        result.Success.Should().BeTrue(result.Error);
        result.AssemblyHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Compile_succeeds_for_paginated_response_handler_body()
    {
        const string source = """
            return new BiUM.Contract.Models.Api.PaginatedApiResponse<object>(
                Array.Empty<object>(),
                0,
                1,
                10);
            """;

        var result = DynamicApiCompiler.Compile(source, "paginated");

        result.Success.Should().BeTrue(result.Error);
    }

    [Fact]
    public void Compile_succeeds_for_non_generic_PaginatedApiResponse_handler_body()
    {
        const string source = """
            return new PaginatedApiResponse(Array.Empty<object>(), 0, 1, 10);
            """;

        var prepared = DynamicApiHandlerSourceNormalizer.NormalizeHandlerResponses(source);
        var result = DynamicApiCompiler.Compile(prepared, "paginated-non-generic");

        result.Success.Should().BeTrue(result.Error);
    }

    [Fact]
    public void Compile_succeeds_for_normalized_domain_db_and_non_generic_ApiResponse()
    {
        const string source = """
            var count = await ctx.Db.DomainDynamicApis.CountAsync(cancellationToken);
            return new ApiResponse { Value = count };
            """;

        var prepared = DynamicApiHandlerSourceNormalizer.PrepareForCompile(
            source,
            typeof(Helpers.TestBiDbContext).FullName,
            usesDynamicTables: false);

        var result = DynamicApiCompiler.Compile(new DynamicApiCompileRequest
        {
            HandlerSourceCode = prepared,
            TypeNameSeed = "normalized-domain-db",
            UsesDynamicTables = false,
            DomainDbContextTypeFullName = typeof(Helpers.TestBiDbContext).FullName
        });

        result.Success.Should().BeTrue(result.Error);
    }
}