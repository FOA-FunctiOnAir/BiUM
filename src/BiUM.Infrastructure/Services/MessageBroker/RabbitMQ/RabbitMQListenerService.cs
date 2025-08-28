using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQListenerService : BackgroundService
{
    private static readonly ConcurrentDictionary<(Type handlerIface, Type eventType), Func<object, object, CancellationToken, Task>> _invokerCache = new();

    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQClient _client;
    private readonly ILogger<RabbitMQListenerService> _logger;

    public RabbitMQListenerService(
        IServiceProvider serviceProvider,
        IRabbitMQClient client,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        ILogger<RabbitMQListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
    }

    private static Func<object, object, CancellationToken, Task> BuildInvokerWithCreateDelegate(Type handlerInterface, Type eventType)
    {
        var method = handlerInterface.GetMethod("HandleAsync", [eventType, typeof(CancellationToken)])
                     ?? throw new InvalidOperationException($"HandleAsync({eventType.Name}, CancellationToken) not found on {handlerInterface.Name}");

        var typedDelegateType = typeof(Func<,,,>).MakeGenericType(handlerInterface, eventType, typeof(CancellationToken), typeof(Task));
        var typedDelegate = method.CreateDelegate(typedDelegateType);

        var h = Expression.Parameter(typeof(object), "h");
        var m = Expression.Parameter(typeof(object), "m");
        var ct = Expression.Parameter(typeof(CancellationToken), "ct");

        var typedDelConst = Expression.Constant(typedDelegate, typedDelegateType);
        var castH = Expression.Convert(h, handlerInterface);
        var castM = Expression.Convert(m, eventType);

        var invokeMethod = typedDelegateType.GetMethod("Invoke")!;
        var invokeCall = Expression.Call(typedDelConst, invokeMethod, castH, castM, ct);

        var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task>>(invokeCall, h, m, ct);

        return lambda.Compile();
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_rabbitMQOptions.Enable)
            return Task.CompletedTask;

        var handlerTypes = GetAllHandlerTypes();

        foreach (var handlerType in handlerTypes)
        {
            var eventType = handlerType.GetInterface("IEventHandler`1")!.GenericTypeArguments[0];
            var handlerInterface = typeof(IEventHandler<>).MakeGenericType(eventType);

            var invoker = _invokerCache.GetOrAdd(
                (handlerInterface, eventType),
                key => BuildInvokerWithCreateDelegate(key.handlerIface, key.eventType)
            );

            _client.StartConsuming(eventType, async obj =>
            {
                _logger.LogWarning("Event StartConsuming started for {EventType}", eventType.FullName);
                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var handler = scope.ServiceProvider.GetRequiredService(handlerInterface);

                    await invoker(handler, obj, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler failed for {EventType}", eventType.FullName);
                }
            });
        }

        return Task.CompletedTask;
    }

    protected /*override*/ Task ExecuteAsync_Old(CancellationToken stoppingToken)
    {
        if (!_rabbitMQOptions.Enable)
            return Task.CompletedTask;

        var handlerTypes = GetAllHandlerTypes();

        foreach (var handlerType in handlerTypes)
        {
            var eventType = handlerType.GetInterface("IEventHandler`1")!.GenericTypeArguments[0];

            var handlerGenericType = typeof(IEventHandler<>).MakeGenericType(eventType);

            _client.StartConsuming(eventType, async (obj) =>
            {
                _logger.LogWarning("Event StartConsuming started for {EventType}", eventType.FullName);

                try
                {
                    using var scope = _serviceProvider.CreateScope();

                    var handler = scope.ServiceProvider.GetRequiredService(handlerGenericType);
                    var method = handlerType.GetMethod("HandleAsync")!;

                    await (Task)method.Invoke(handler, [obj, stoppingToken])!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Event handler failed for {EventType}", eventType.FullName);
                }
            });
        }

        return Task.CompletedTask;
    }

    private static Type[] GetAllHandlerTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
            .ToArray();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}