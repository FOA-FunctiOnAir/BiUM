using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQListenerService : BackgroundService
{
    private static readonly ConcurrentDictionary<(Type handlerIface, Type eventType), Func<object, object, CancellationToken, Task>> _invokerCache = new();

    private readonly BiAppOptions _biAppOptions;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQClient _client;
    private readonly ILogger<RabbitMQListenerService> _logger;
    private readonly ConcurrentDictionary<Type, string> _consumerTags = new();

    public RabbitMQListenerService(
        IServiceProvider serviceProvider,
        IRabbitMQClient client,
        IOptions<BiAppOptions> biAppOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        ILogger<RabbitMQListenerService> logger)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _biAppOptions = biAppOptions.Value;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_rabbitMQOptions.Enable) return;

        var handlerTypes = RabbitMQUtils.GetAllHandlerTypes();
        var consumerName = _biAppOptions.Domain;

        foreach (var handlerType in handlerTypes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var (eventType, handlerInterface) = RabbitMQUtils.GetInterfaceAndGenericType(handlerType);

            var invoker = _invokerCache.GetOrAdd(
                (handlerInterface, eventType),
                key => BuildInvokerWithCreateDelegate(key.handlerIface, key.eventType)
            );

            try
            {
                await _client.StartConsumingAsync(eventType, async obj =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService(handlerInterface);
                    await invoker(handler, obj, cancellationToken).ConfigureAwait(false);
                }, consumerName);

                _logger.LogInformation("Started consuming events for {EventType}", eventType.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start consuming events for {EventType}", eventType.Name);
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
}
