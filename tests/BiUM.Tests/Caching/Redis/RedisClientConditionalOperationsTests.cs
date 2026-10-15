using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Services;
using BiUM.Infrastructure.Services.Caching.Redis;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using System.Text.Json;
using Xunit;

namespace BiUM.Tests.Caching.Redis;

public sealed class RedisClientConditionalOperationsTests
{
    private readonly Mock<IDatabase> _database = new();

    private RedisClient CreateClient() =>
        new(_database.Object, new RedisOptions { Enable = true }, "Default", Mock.Of<IDateTimeService>(), NullLogger<RedisClient>.Instance);

    [Fact]
    public async Task AddIfNotExistsAsync_sets_value_only_when_missing_with_expiry()
    {
        var token = Guid.NewGuid();
        var expiry = TimeSpan.FromSeconds(55);

        _database
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var acquired = await CreateClient().AddIfNotExistsAsync("lock:key", token, expiry);

        acquired.Should().BeTrue();
        _database.Verify(d => d.StringSetAsync(
            It.Is<RedisKey>(key => key == "lock:key"),
            It.Is<RedisValue>(value => value == JsonSerializer.Serialize(token, (JsonSerializerOptions?)null)),
            expiry,
            When.NotExists,
            CommandFlags.None), Times.Once);
    }

    [Fact]
    public async Task AddIfNotExistsAsync_returns_false_when_key_exists()
    {
        _database
            .Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), When.NotExists, It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var acquired = await CreateClient().AddIfNotExistsAsync("lock:key", Guid.NewGuid(), TimeSpan.FromSeconds(5));

        acquired.Should().BeFalse();
    }

    [Fact]
    public async Task AddAllAsync_writes_every_item_in_one_batch()
    {
        var batch = new Mock<IBatch>();

        batch
            .Setup(b => b.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _database.Setup(d => d.CreateBatch(It.IsAny<object?>())).Returns(batch.Object);

        await CreateClient().AddAllAsync(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }, TimeSpan.FromMinutes(1));

        batch.Verify(b => b.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), TimeSpan.FromMinutes(1), When.Always, CommandFlags.None), Times.Exactly(2));
        batch.Verify(b => b.Execute(), Times.Once);
    }

    [Fact]
    public async Task AddAllAsync_skips_redis_for_empty_input()
    {
        await CreateClient().AddAllAsync(new Dictionary<string, int>(), TimeSpan.FromMinutes(1));

        _database.Verify(d => d.CreateBatch(It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public async Task RemoveIfEqualsAsync_passes_key_and_expected_value_to_script()
    {
        var token = Guid.NewGuid();
        RedisKey[]? keys = null;
        RedisValue[]? values = null;

        _database
            .Setup(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, k, v, _) =>
            {
                keys = k;
                values = v;
            })
            .ReturnsAsync(RedisResult.Create(1));

        var removed = await CreateClient().RemoveIfEqualsAsync("lock:key", token);

        removed.Should().BeTrue();
        keys.Should().Equal((RedisKey)"lock:key");
        values.Should().ContainSingle();
        values![0].ToString().Should().Be(JsonSerializer.Serialize(token));
        _database.Verify(d => d.ScriptEvaluateAsync(RedisScripts.RemoveIfEqualScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveIfEqualsAsync_returns_false_when_script_deletes_nothing()
    {
        _database
            .Setup(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(0));

        var removed = await CreateClient().RemoveIfEqualsAsync("lock:key", Guid.NewGuid());

        removed.Should().BeFalse();
    }

    [Fact]
    public async Task ReplaceIfEqualsAsync_passes_value_expected_and_expiry_in_milliseconds()
    {
        RedisValue[]? values = null;

        _database
            .Setup(d => d.ScriptEvaluateAsync(It.IsAny<string>(), It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()))
            .Callback<string, RedisKey[], RedisValue[], CommandFlags>((_, _, v, _) => values = v)
            .ReturnsAsync(RedisResult.Create(1));

        var replaced = await CreateClient().ReplaceIfEqualsAsync("key", "new", "old", TimeSpan.FromSeconds(2));

        replaced.Should().BeTrue();
        values.Should().HaveCount(3);
        values![0].ToString().Should().Be("new");
        values[1].ToString().Should().Be("old");
        values[2].ToString().Should().Be("2000");
        _database.Verify(d => d.ScriptEvaluateAsync(RedisScripts.ReplaceIfEqualScript, It.IsAny<RedisKey[]>(), It.IsAny<RedisValue[]>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}