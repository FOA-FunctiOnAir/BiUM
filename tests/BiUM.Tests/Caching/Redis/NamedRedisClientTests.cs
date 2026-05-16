using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace BiUM.Tests.Caching.Redis;

public sealed class NamedRedisClientTests
{
    private const string GatewayClientKey = "Gateway";

    private static string RedisChild(string clientKey, string suffix) =>
        $"{RedisOptions.Name}:{clientKey}:{suffix}";

    private static IConfiguration BuildConfig(
        bool redisEnabled,
        string defaultConnectionString,
        string gatewayConnectionString) =>
        BuildConfig(redisEnabled, redisEnabled, defaultConnectionString, gatewayConnectionString);

    private static IConfiguration BuildConfig(
        bool defaultEnabled,
        bool gatewayEnabled,
        string defaultConnectionString,
        string gatewayConnectionString)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RedisChild(RedisOptions.DefaultClientKey, nameof(RedisOptions.Enable))] =
                    defaultEnabled ? "true" : "false",
                [RedisChild(RedisOptions.DefaultClientKey, nameof(RedisOptions.ConnectionString))] =
                    defaultConnectionString,
                [RedisChild(GatewayClientKey, nameof(RedisOptions.Enable))] = gatewayEnabled ? "true" : "false",
                [RedisChild(GatewayClientKey, nameof(RedisOptions.ConnectionString))] =
                    gatewayConnectionString
            })
            .Build();
    }

    private static void AddTestCoreServices(IServiceCollection services, IConfiguration config)
    {
        services.AddOptions();
        services.AddSingleton(config);
        services.AddLogging(b =>
        {
            b.SetMinimumLevel(LogLevel.None);
            b.AddProvider(NullLoggerProvider.Instance);
        });

        var dateTimeMock = new Mock<IDateTimeService>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);
        dateTimeMock.Setup(d => d.OffsetNow).Returns(DateTimeOffset.UtcNow);
        dateTimeMock.Setup(d => d.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetToday).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.TimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetTimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));
        services.AddSingleton(dateTimeMock.Object);
    }

    private static ServiceProvider BuildProvider(IConfiguration config, bool includeGateway)
    {
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        if (includeGateway)
        {
            services.AddBiUMRedisClients(config, GatewayClientKey);
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Named_default_and_gateway_options_bind_to_distinct_connection_strings_when_gateway_requested()
    {
        var defaultCs = "localhost:6379,abortConnect=false,defaultDatabase=0";
        var gatewayCs = "localhost:6379,abortConnect=false,defaultDatabase=1";
        var config = BuildConfig(defaultEnabled: false, gatewayEnabled: false, defaultCs, gatewayCs);
        using var sp = BuildProvider(config, includeGateway: true);
        var monitor = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<RedisOptions>>();

        var a = monitor.Get(RedisOptions.DefaultClientKey);
        var b = monitor.Get(GatewayClientKey);

        a.ConnectionString.Should().Be(defaultCs);
        b.ConnectionString.Should().Be(gatewayCs);

        var dbDefault = ConfigurationOptions.Parse(a.ConnectionString!).DefaultDatabase;
        var dbGateway = ConfigurationOptions.Parse(b.ConnectionString!).DefaultDatabase;
        dbDefault.Should().Be(0);
        dbGateway.Should().Be(1);
    }

    [Fact]
    public void Unkeyed_and_gateway_keyed_IRedisClient_are_registered_separately_when_both_enabled()
    {
        var config = BuildConfig(defaultEnabled: true, gatewayEnabled: true, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        services.AddBiUMRedisClients(config, GatewayClientKey);

        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(1);
        services.Count(static d =>
                d.ServiceType == typeof(IRedisClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);
    }

    [Fact]
    public void When_default_Disabled_GetRequired_IRedisClient_fails_even_if_gateway_client_is_registered()
    {
        var config = BuildConfig(defaultEnabled: false, gatewayEnabled: true, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        services.AddBiUMRedisClients(config, GatewayClientKey);

        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(0);
        services.Count(static d =>
                d.ServiceType == typeof(IRedisClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);

        using var sp = services.BuildServiceProvider();
        var act = () => sp.GetRequiredService<IRedisClient>();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void When_gateway_Disabled_keyed_registration_is_skipped_but_options_remain()
    {
        var config = BuildConfig(defaultEnabled: true, gatewayEnabled: false, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        services.AddBiUMRedisClients(config, GatewayClientKey);

        using var sp = services.BuildServiceProvider();
        var act = () => sp.GetRequiredKeyedService<IRedisClient>(GatewayClientKey);
        act.Should().Throw<InvalidOperationException>();

        var monitor = sp.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<RedisOptions>>();
        monitor.Get(GatewayClientKey).Enable.Should().BeFalse();
        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(1);
    }

    [Fact]
    public void AddBiUMRedisClients_throws_when_additional_name_is_default_key()
    {
        var config = BuildConfig(redisEnabled: false, "a", "b");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);

        var act = () =>
            services.AddBiUMRedisClients(config, RedisOptions.DefaultClientKey);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddBiUMRedisClients_throws_when_additional_name_is_empty()
    {
        var config = BuildConfig(redisEnabled: false, "a", "b");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);

        var act = () => services.AddBiUMRedisClients(config, "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Unknown_keyed_name_throws_invalid_operation_when_not_registered()
    {
        var config = BuildConfig(redisEnabled: false, "localhost:1", "localhost:2");
        using var sp = BuildProvider(config, includeGateway: false);

        var act = () => sp.GetRequiredKeyedService<IRedisClient>("NoSuchRedis");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddBiUMRedisClients_default_path_when_disabled_does_not_register_unkeyed()
    {
        var config = BuildConfig(redisEnabled: false, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);

        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(0);

        using var sp = services.BuildServiceProvider();
        var act = () => sp.GetRequiredService<IRedisClient>();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddBiUMRedisClients_default_path_is_idempotent_when_enabled()
    {
        var config = BuildConfig(redisEnabled: true, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        services.AddBiUMRedisClients(config);

        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(1);
    }

    [Fact]
    public void AddBiUMRedisClients_additional_client_is_idempotent_when_enabled()
    {
        var config = BuildConfig(defaultEnabled: false, gatewayEnabled: true, "localhost:1", "localhost:2");
        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config, GatewayClientKey);
        services.AddBiUMRedisClients(config, GatewayClientKey);

        services.Count(static d =>
                d.ServiceType == typeof(IRedisClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);
    }

    [Fact]
    public void Multiple_AddBiUMRedisClients_calls_can_register_distinct_additional_clients()
    {
        const string secondKey = "Other";
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RedisChild(RedisOptions.DefaultClientKey, nameof(RedisOptions.Enable))] = "true",
                [RedisChild(RedisOptions.DefaultClientKey, nameof(RedisOptions.ConnectionString))] =
                    "localhost:1",
                [RedisChild(GatewayClientKey, nameof(RedisOptions.Enable))] = "true",
                [RedisChild(GatewayClientKey, nameof(RedisOptions.ConnectionString))] = "localhost:2",
                [RedisChild(secondKey, nameof(RedisOptions.Enable))] = "true",
                [RedisChild(secondKey, nameof(RedisOptions.ConnectionString))] = "localhost:3"
            })
            .Build();

        var services = new ServiceCollection();
        AddTestCoreServices(services, config);
        services.AddBiUMRedisClients(config);
        services.AddBiUMRedisClients(config, GatewayClientKey);
        services.AddBiUMRedisClients(config, secondKey);

        services.Count(static d =>
                d.ServiceType == typeof(IRedisClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);
        services.Count(static d =>
                d.ServiceType == typeof(IRedisClient) && d.IsKeyedService && Equals(d.ServiceKey, secondKey))
            .Should().Be(1);
        services.Count(static d => d.ServiceType == typeof(IRedisClient) && !d.IsKeyedService).Should().Be(1);
    }
}