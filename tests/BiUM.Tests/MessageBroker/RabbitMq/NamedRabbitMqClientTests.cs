using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BiUM.Tests.MessageBroker.RabbitMq;

public sealed class NamedRabbitMqClientTests
{
    private const string GatewayClientKey = "Gateway";

    private static string RabbitChild(string clientKey, string suffix) =>
        $"{RabbitMqOptions.Name}:{clientKey}:{suffix}";

    private static IConfiguration BuildConfig(
        bool defaultEnabled,
        string defaultHostname,
        bool gatewayEnabled,
        string gatewayHostname)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [RabbitChild(RabbitMqOptions.DefaultClientKey, nameof(RabbitMqOptions.Enable))] =
                    defaultEnabled ? "true" : "false",
                [RabbitChild(RabbitMqOptions.DefaultClientKey, nameof(RabbitMqOptions.Hostname))] =
                    defaultHostname,
                [RabbitChild(RabbitMqOptions.DefaultClientKey, nameof(RabbitMqOptions.UserName))] = "u1",
                [RabbitChild(RabbitMqOptions.DefaultClientKey, nameof(RabbitMqOptions.Password))] = "p1",
                [RabbitChild(GatewayClientKey, nameof(RabbitMqOptions.Enable))] =
                    gatewayEnabled ? "true" : "false",
                [RabbitChild(GatewayClientKey, nameof(RabbitMqOptions.Hostname))] = gatewayHostname,
                [RabbitChild(GatewayClientKey, nameof(RabbitMqOptions.UserName))] = "u2",
                [RabbitChild(GatewayClientKey, nameof(RabbitMqOptions.Password))] = "p2"
            })
            .Build();
    }

    private static ServiceCollection MinimalServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton(configuration);
        return services;
    }

    [Fact]
    public void AddBiUMRabbitMqClients_throws_when_default_section_missing()
    {
        var config = new ConfigurationBuilder().Build();
        var services = MinimalServices(config);

        var act = () => services.AddBiUMRabbitMqClients(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*required*");
    }

    [Fact]
    public void AddBiUMRabbitMqClients_throws_when_default_disabled()
    {
        var config =
            BuildConfig(defaultEnabled: false, "localhost", gatewayEnabled: false, gatewayHostname: "ignored");
        var services = MinimalServices(config);

        var act = () => services.AddBiUMRabbitMqClients(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*enabled*");
    }

    [Fact]
    public void AddBiUMRabbitMqClients_throws_when_hostname_empty()
    {
        var config =
            BuildConfig(defaultEnabled: true, "  ", gatewayEnabled: false, gatewayHostname: "ignored");
        var services = MinimalServices(config);

        var act = () => services.AddBiUMRabbitMqClients(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Hostname*");
    }

    [Fact]
    public void AddBiUMRabbitMqClients_throws_when_additional_name_is_default_key()
    {
        var config =
            BuildConfig(defaultEnabled: true, "localhost", gatewayEnabled: true, gatewayHostname: "other");
        var services = MinimalServices(config);

        var act = () =>
            services.AddBiUMRabbitMqClients(config, RabbitMqOptions.DefaultClientKey);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddBiUMRabbitMqClients_throws_when_additional_name_is_whitespace()
    {
        var config =
            BuildConfig(defaultEnabled: true, "localhost", gatewayEnabled: true, gatewayHostname: "other");
        var services = MinimalServices(config);

        var act = () => services.AddBiUMRabbitMqClients(config, "  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Named_default_and_gateway_options_bind_distinct_hostname_when_gateway_requested()
    {
        var config = BuildConfig(
            defaultEnabled: true,
            "host-a",
            gatewayEnabled: true,
            gatewayHostname: "host-b");

        var services = MinimalServices(config);
        services.AddBiUMRabbitMqClients(config);
        services.AddBiUMRabbitMqClients(config, GatewayClientKey);

        using var sp = services.BuildServiceProvider();
        var monitor = sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>();
        monitor.Get(RabbitMqOptions.DefaultClientKey).Hostname.Should().Be("host-a");
        monitor.Get(GatewayClientKey).Hostname.Should().Be("host-b");
    }

    [Fact]
    public void When_both_enabled_unkeyed_and_keyed_clients_are_registered()
    {
        var config = BuildConfig(
            defaultEnabled: true,
            "localhost",
            gatewayEnabled: true,
            gatewayHostname: "gateway.local");

        var services = MinimalServices(config);
        services.AddBiUMRabbitMqClients(config);
        services.AddBiUMRabbitMqClients(config, GatewayClientKey);

        services.Count(static d => d.ServiceType == typeof(IRabbitMQClient) && !d.IsKeyedService).Should().Be(1);
        services.Count(static d =>
                d.ServiceType == typeof(IRabbitMQClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);
    }

    [Fact]
    public void When_gateway_Disabled_keyed_registration_is_skipped_but_options_remain()
    {
        var config = BuildConfig(
            defaultEnabled: true,
            "localhost",
            gatewayEnabled: false,
            gatewayHostname: "gateway.local");

        var services = MinimalServices(config);
        services.AddBiUMRabbitMqClients(config);
        services.AddBiUMRabbitMqClients(config, GatewayClientKey);

        services.Count(static d =>
                d.ServiceType == typeof(IRabbitMQClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(0);
        services.Count(static d => d.ServiceType == typeof(IRabbitMQClient) && !d.IsKeyedService).Should().Be(1);

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IOptionsMonitor<RabbitMqOptions>>().Get(GatewayClientKey).Enable.Should()
            .BeFalse();
    }

    [Fact]
    public void AddBiUMRabbitMqClients_is_idempotent_when_enabled()
    {
        var config =
            BuildConfig(defaultEnabled: true, "localhost", gatewayEnabled: false, gatewayHostname: "ignored");
        var services = MinimalServices(config);
        services.AddBiUMRabbitMqClients(config);
        services.AddBiUMRabbitMqClients(config);

        services.Count(static d => d.ServiceType == typeof(IRabbitMQClient) && !d.IsKeyedService).Should().Be(1);
    }

    [Fact]
    public void AddBiUMRabbitMq_additional_client_is_idempotent_when_enabled()
    {
        var config = BuildConfig(
            defaultEnabled: true,
            "localhost",
            gatewayEnabled: true,
            gatewayHostname: "gateway.local");
        var services = MinimalServices(config);
        services.AddBiUMRabbitMqClients(config, GatewayClientKey);
        services.AddBiUMRabbitMqClients(config, GatewayClientKey);

        services.Count(static d =>
                d.ServiceType == typeof(IRabbitMQClient) && d.IsKeyedService && Equals(d.ServiceKey, GatewayClientKey))
            .Should().Be(1);
    }
}