using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal class RabbitMQHealthCheck : IHealthCheck
{
    private readonly RabbitMQOptions _options;
    private readonly RabbitMQConnectionProvider _connectionProvider;
    private readonly IRabbitMQClient? _client;

    public RabbitMQHealthCheck(
        RabbitMQConnectionProvider connectionProvider,
        IOptions<RabbitMQOptions> optionsAccessor,
        IRabbitMQClient? client = null)
    {
        _options = optionsAccessor.Value;
        _connectionProvider = connectionProvider;
        _client = client;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enable)
        {
            return HealthCheckResult.Healthy("RabbitMQ is disabled");
        }

        if (_client is null)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ client is not registered");
        }

        try
        {
            var connection = await _connectionProvider.GetConsumerConnectionAsync();

            return
                connection.IsOpen
                    ? HealthCheckResult.Healthy("RabbitMQ connection is working")
                    : HealthCheckResult.Degraded("RabbitMQ connection is not open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "RabbitMQ health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["hostname"] = _options.Hostname ?? "unknown",
                    ["port"] = _options.Port ?? 5672,
                    ["virtualHost"] = _options.VirtualHost ?? "/"
                });
        }
    }
}