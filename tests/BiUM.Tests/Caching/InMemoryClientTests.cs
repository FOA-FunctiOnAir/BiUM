using BiUM.Infrastructure.Services.Caching.InMemory;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.Caching;

public sealed class InMemoryClientTests
{
    private sealed record Lock(string Owner);

    private static InMemoryClient Create() => new(new MemoryCache(new MemoryCacheOptions()), new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task AddAndGet_ShouldStoreValues()
    {
        using var client = Create();

        (await client.AddAsync("key", 42)).Should().BeTrue();

        var item = await client.GetAsync<int>("key");

        item.Value.Should().Be(42);
        item.IsNull.Should().BeFalse();
        (await client.ExistsAsync("key")).Should().BeTrue();
        (await client.GetAsync<string>("missing")).IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveIfEquals_ShouldOnlyRemoveMatchingValue()
    {
        using var client = Create();

        await client.AddAsync("lock", new Lock("a"));

        (await client.RemoveIfEqualsAsync("lock", new Lock("b"))).Should().BeFalse();
        (await client.ExistsAsync("lock")).Should().BeTrue();
        (await client.RemoveIfEqualsAsync("lock", new Lock("a"))).Should().BeTrue();
        (await client.ExistsAsync("lock")).Should().BeFalse();
        (await client.RemoveIfEqualsAsync("lock", new Lock("a"))).Should().BeFalse();
    }

    [Fact]
    public async Task ReplaceIfEquals_ShouldOnlyReplaceMatchingValue()
    {
        using var client = Create();

        await client.AddAsync("key", "v1");

        (await client.ReplaceIfEqualsAsync("key", "v2", "other")).Should().BeFalse();
        (await client.GetAsync<string>("key")).Value.Should().Be("v1");
        (await client.ReplaceIfEqualsAsync("key", "v2", "v1")).Should().BeTrue();
        (await client.GetAsync<string>("key")).Value.Should().Be("v2");
    }

    [Fact]
    public async Task Expiration_ShouldBeTrackedAndApplied()
    {
        using var client = Create();

        await client.AddAsync("short", "value", TimeSpan.FromMilliseconds(50));

        (await client.GetExpirationAsync("short")).Should().NotBeNull();

        await Task.Delay(120);

        (await client.GetAsync<string>("short")).IsNull.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrCreate_ShouldCallFactoryOnce()
    {
        using var client = Create();
        var calls = 0;

        Task<string?> Factory()
        {
            calls++;

            return Task.FromResult<string?>("created");
        }

        (await client.GetOrCreateAsync("key", Factory)).Should().Be("created");
        (await client.GetOrCreateAsync("key", Factory)).Should().Be("created");
        calls.Should().Be(1);
    }

    [Fact]
    public async Task ScanKeys_ShouldMatchPattern()
    {
        using var client = Create();

        await client.AddAsync("biapp:configuration:service:1", 1);
        await client.AddAsync("biapp:configuration:service:2", 2);
        await client.AddAsync("biapp:parameters:value:1", 3);

        (await client.ScanKeysAsync("biapp:configuration:*")).Should().BeEquivalentTo(["biapp:configuration:service:1", "biapp:configuration:service:2"]);
    }
}