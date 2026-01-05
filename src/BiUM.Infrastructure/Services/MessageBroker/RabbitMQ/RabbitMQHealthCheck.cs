using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly RabbitMQOptions _options;
    private readonly IRabbitMQClient? _client;

    public RabbitMQHealthCheck(IOptions<RabbitMQOptions> options, IRabbitMQClient? client = null)
    {
        _options = options.Value;
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
            return HealthCheckResult.Unhealthy("RabbitMQ client is not available");
        }

        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri($"amqp://{_options.UserName}:{_options.Password}@{_options.Hostname}:{_options.Port}/{_options.VirtualHost}"),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);

            if (connection.IsOpen)
            {
                return HealthCheckResult.Healthy("RabbitMQ connection is healthy");
            }

            return HealthCheckResult.Degraded("RabbitMQ connection is not open");
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
