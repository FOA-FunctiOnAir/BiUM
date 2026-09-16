using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace BiUM.Tests.DynamicApi;

public class DynamicApiContextSurfaceTests
{
    [Fact]
    public void Execution_context_does_not_expose_connection_string_on_public_interface()
    {
        var ctx = DynamicApiExecutionContextTestHelper.Create(Mock.Of<BiUM.Specialized.Database.IDbContext>());

        ctx.Should().BeAssignableTo<IDynamicApiExecutionContext>();
        typeof(IDynamicApiExecutionContext).GetProperty(nameof(DynamicApiExecutionContext.ConnectionString)).Should().BeNull();
        typeof(IDynamicApiExecutionContext).GetProperty(nameof(DynamicApiExecutionContext.DatabaseType)).Should().BeNull();
    }

    [Fact]
    public async Task Memory_cache_uses_dynamic_api_id_key_prefix()
    {
        var apiId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var ctx = DynamicApiExecutionContextTestHelper.Create(
            Mock.Of<BiUM.Specialized.Database.IDbContext>(),
            dynamicApiId: apiId,
            memoryCache: memoryCache);

        await ctx.MemoryCache.SetAsync("result", 42);

        memoryCache.TryGetValue($"{apiId}-result", out int value).Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void Cache_ttl_normalizes_to_one_day_max()
    {
        DynamicApiCacheTtl.Normalize(null).Should().Be(TimeSpan.FromDays(1));
        DynamicApiCacheTtl.Normalize(TimeSpan.FromHours(2)).Should().Be(TimeSpan.FromHours(2));
        DynamicApiCacheTtl.Normalize(TimeSpan.FromDays(3)).Should().Be(TimeSpan.FromDays(1));
    }
}