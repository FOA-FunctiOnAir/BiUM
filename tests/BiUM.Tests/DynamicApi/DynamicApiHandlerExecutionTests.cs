using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.DynamicApi;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiHandlerExecutionTests
{
    [Fact]
    public async Task Compiled_handler_executes_and_returns_ApiResponse()
    {
        const string source = """
            return new BiUM.Contract.Models.Api.ApiResponse<int>
            {
                Value = (ctx.PageStart ?? 0) + (ctx.PageSize ?? 10)
            };
            """;

        var compile = DynamicApiCompiler.Compile(source, "page-sum");
        compile.Success.Should().BeTrue();

        var handler = LoadHandler(compile);
        var ctx = new DynamicApiExecutionContext(
            Mock.Of<IDbContext>(),
            new Dictionary<string, object?>(),
            pageStart: 5,
            pageSize: 10,
            correlation: null,
            connectionString: "Data Source=:memory:",
            databaseType: DynamicApiSchemaRules.DbTypePostgresql);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        result.Should().BeOfType<ApiResponse<int>>();
        ((ApiResponse<int>)result).Value.Should().Be(15);
    }

    [Fact]
    public async Task Compiled_handler_returns_PaginatedApiResponse_with_page_context()
    {
        const string source = """
            var pageStart = ctx.PageStart ?? 0;
            var pageSize = ctx.PageSize ?? 10;
            return new PaginatedApiResponse<string>(
                new List<string> { "a", "b" },
                2,
                (pageStart / pageSize) + 1,
                pageSize);
            """;

        var compile = DynamicApiCompiler.Compile(source, "paginated-page-context");
        compile.Success.Should().BeTrue(compile.Error);

        var handler = LoadHandler(compile);
        var ctx = new DynamicApiExecutionContext(
            Mock.Of<IDbContext>(),
            new Dictionary<string, object?>(),
            pageStart: 10,
            pageSize: 10,
            correlation: null,
            connectionString: "Data Source=:memory:",
            databaseType: DynamicApiSchemaRules.DbTypePostgresql);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        var paginated = result.Should().BeOfType<PaginatedApiResponse<string>>().Subject;
        paginated.PageNumber.Should().Be(2);
        paginated.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Compiled_handler_returns_PaginatedApiResponse()
    {
        const string source = """
            return new BiUM.Contract.Models.Api.PaginatedApiResponse<string>(
                new List<string> { "a", "b" },
                2,
                1,
                10);
            """;

        var compile = DynamicApiCompiler.Compile(source, "paginated");
        compile.Success.Should().BeTrue();

        var handler = LoadHandler(compile);
        var ctx = new DynamicApiExecutionContext(
            Mock.Of<IDbContext>(),
            new Dictionary<string, object?>(),
            null,
            null,
            null,
            "Data Source=:memory:",
            DynamicApiSchemaRules.DbTypePostgresql);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        result.Should().BeAssignableTo<ApiResponse>();
        var paginated = result.Should().BeOfType<PaginatedApiResponse<string>>().Subject;
        paginated.Value.Should().BeEquivalentTo(["a", "b"]);
        paginated.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Compiled_handler_queries_DomainDynamicApis_via_ef()
    {
        var correlationProvider = new TestCorrelationContextProvider();
        await using var serviceProvider = BiUMServiceFactory.BuildInMemory(
            correlationProvider,
            databaseName: Guid.NewGuid().ToString());

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDbContext>();

        const string source = """
            var count = await ctx.Db.DomainDynamicApis.CountAsync(cancellationToken);
            return new BiUM.Contract.Models.Api.ApiResponse<int> { Value = count };
            """;

        var compile = DynamicApiCompiler.Compile(source, "ef-count");
        compile.Success.Should().BeTrue(compile.Error);

        var handler = LoadHandler(compile);
        var ctx = new DynamicApiExecutionContext(
            db,
            new Dictionary<string, object?>(),
            null,
            null,
            null,
            "Data Source=:memory:",
            DynamicApiSchemaRules.DbTypePostgresql);

        var result = await handler.ExecuteAsync(ctx, CancellationToken.None);

        ((ApiResponse<int>)result).Value.Should().Be(0);
    }

    [Fact]
    public void Runtime_cache_reuses_same_handler_instance_for_same_key()
    {
        var compile = DynamicApiCompiler.Compile(
            "return new BiUM.Contract.Models.Api.ApiResponse();",
            "cache-key");

        compile.Success.Should().BeTrue();

        var cache = new DynamicApiRuntimeCache(new MemoryCache(new MemoryCacheOptions()));
        var first = cache.GetOrLoad("sample-key", compile.AssemblyBytes!, compile.EntryPointTypeName!);
        var second = cache.GetOrLoad("sample-key", compile.AssemblyBytes!, compile.EntryPointTypeName!);

        first.Should().BeSameAs(second);
    }

    [Fact]
    public void Runtime_cache_invalidates_cached_handler()
    {
        var compile = DynamicApiCompiler.Compile(
            "return new BiUM.Contract.Models.Api.ApiResponse();",
            "invalidate");

        compile.Success.Should().BeTrue();

        var cache = new DynamicApiRuntimeCache(new MemoryCache(new MemoryCacheOptions()));
        var first = cache.GetOrLoad("invalidate-key", compile.AssemblyBytes!, compile.EntryPointTypeName!);
        cache.Invalidate("invalidate-key");
        var second = cache.GetOrLoad("invalidate-key", compile.AssemblyBytes!, compile.EntryPointTypeName!);

        first.Should().NotBeSameAs(second);
    }

    private static IDynamicApiHandler LoadHandler(DynamicApiCompileResult compile)
    {
        var cache = new DynamicApiRuntimeCache(new MemoryCache(new MemoryCacheOptions()));
        return cache.GetOrLoad(
            Guid.NewGuid().ToString(),
            compile.AssemblyBytes!,
            compile.EntryPointTypeName!);
    }
}