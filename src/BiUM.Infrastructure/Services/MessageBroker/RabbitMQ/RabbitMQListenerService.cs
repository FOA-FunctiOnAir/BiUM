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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_rabbitMQOptions.Enable)
            return Task.CompletedTask;

        var handlerTypes = GetAllHandlerTypes();

        Console.WriteLine("ExecuteAsync " + handlerTypes.Count.ToString() + " adet");

        foreach (var handlerType in handlerTypes)
        {
            var eventType = handlerType.GetInterface("IEventHandler`1")!.GenericTypeArguments[0];

            _client.StartConsuming(eventType, async (obj) =>
            {
                using var scope = _serviceProvider.CreateScope();

                try
                {
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);
                    var method = handlerType.GetMethod("HandleAsync")!;

                    await (Task)method.Invoke(handler, [obj, CancellationToken.None])!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler failed for {EventType}", eventType.FullName);
                }
            });
        }

        return Task.CompletedTask;
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