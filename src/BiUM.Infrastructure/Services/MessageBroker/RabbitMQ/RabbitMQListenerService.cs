using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQListenerService : BackgroundService
{
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQClient _client;
    private readonly ILogger<RabbitMQListenerService> _logger;

    public RabbitMQListenerService(IServiceProvider serviceProvider, IRabbitMQClient client, IOptions<RabbitMQOptions> rabbitMQOptions, ILogger<RabbitMQListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var handlerTypes = GetAllHandlerTypes();

        foreach (var handlerType in handlerTypes)
        {
            var eventType = handlerType.GetInterface("IEventHandler`1")!.GenericTypeArguments[0];

            _ = Task.Run(() => ListenToEvent(eventType, stoppingToken), stoppingToken);
        }
    }

    private async Task ListenToEvent(Type eventType, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _client.ReceiveMessageAsync(eventType, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
                var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                var method = handlerType.GetMethod("HandleAsync")!;

                await (Task)method.Invoke(handler, new[] { message, stoppingToken })!;
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process event from {FullName}", eventType.FullName);

                await Task.Delay(1000);
            }
        }
    }

    private static List<Type> GetAllHandlerTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
            .ToList();
    }
}