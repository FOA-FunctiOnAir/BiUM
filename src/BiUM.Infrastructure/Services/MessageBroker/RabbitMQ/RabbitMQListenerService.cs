using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQListenerService : BackgroundService
{
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IRabbitMQClient _rabbitMqClient;
    private readonly ILogger<RabbitMQListenerService> _logger;

    public RabbitMQListenerService(
        IRabbitMQClient rabbitMqClient,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        ILogger<RabbitMQListenerService> logger)
    {
        _rabbitMqClient = rabbitMqClient;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        foreach (var (_, @interface, @event) in RabbitMQUtils.GetAllHandlers())
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await _rabbitMqClient.StartConsumingAsync(@event, @interface, cancellationToken);

                _logger.LogInformation("Started consuming events for {EventType}", @event.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming events for {EventType}", @event.Name);
            }
        }

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQListenerService cancellation requested");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping RabbitMQListenerService...");

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("RabbitMQListenerService stopped");
    }
}