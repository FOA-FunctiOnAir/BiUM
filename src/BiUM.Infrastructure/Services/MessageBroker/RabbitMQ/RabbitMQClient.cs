using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.Extensions;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Models;
using BiUM.Core.Models.MessageBroker.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQClient : IRabbitMQClient, IAsyncDisposable
{
    private const string RetryCountHeader = "x-retry-count";
    private const string OriginalQueueHeader = "x-original-queue";

    private readonly BiAppOptions _biAppOptions;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly ILogger<RabbitMQClient> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConnectionFactory? _factory;

    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _channelLock = new(1, 1);
    private readonly ConcurrentDictionary<string, bool> _declaredExchanges = new();
    private readonly ConcurrentDictionary<string, bool> _declaredQueues = new();

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQClient(
        IOptions<BiAppOptions> biAppOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        ILogger<RabbitMQClient> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _biAppOptions = biAppOptions.Value;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;

        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        _factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{_rabbitMQOptions.UserName}:{_rabbitMQOptions.Password}@{_rabbitMQOptions.Hostname}:{_rabbitMQOptions.Port}/{_rabbitMQOptions.VirtualHost}"),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(_rabbitMQOptions.NetworkRecoveryIntervalSeconds),
            TopologyRecoveryEnabled = true
        };
    }

    public async Task PublishAsync<T>(T message)
        where T : IBaseEvent
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        EnsureCorrelationContext(message);
        EnsureEventTimestamps(message);

        var channel = await GetChannelAsync();

        var type = message.GetType();
        var attr = type.GetCustomAttribute<EventAttribute>();
        var routingKey = type.Name.ToSnakeCase();

        if (attr?.Mode == EventDeliveryMode.Publish)
        {
            var exchange = RabbitMQUtils.GetOwner(attr, _biAppOptions);

            await EnsureExchangeDeclaredAsync(exchange, ExchangeType.Direct);

            var json = JsonSerializer.Serialize(message, message.GetType());
            var body = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published {EventType} to exchange {Exchange} with rk {RoutingKey}", type.Name, exchange, routingKey);

            return;
        }
        else if (attr?.Mode == EventDeliveryMode.Targeted)
        {
            var queueName = RabbitMQUtils.GetQueueName(type, _biAppOptions);

            await EnsureQueueDeclaredAsync(queueName);

            var json = JsonSerializer.Serialize(message, message.GetType());
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: props, body: body);
        }
        else
        {
            throw new InvalidOperationException($"EventDeliveryMode {attr?.Mode} is not supported for publishing without target.");
        }
    }

    public async Task PublishAsync<T>(string target, T message)
        where T : IBaseEvent
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        EnsureCorrelationContext(message);
        EnsureEventTimestamps(message);

        var type = typeof(T);
        var attr = type.GetCustomAttribute<EventAttribute>();

        if (attr?.Mode == EventDeliveryMode.Publish)
        {
            await PublishAsync(message);

            return;
        }
        else if (attr?.Mode == EventDeliveryMode.Targeted)
        {
            var channel = await GetChannelAsync();
            var queueName = RabbitMQUtils.GetQueueName(type, _biAppOptions, target);

            await EnsureQueueDeclaredAsync(queueName);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties { Persistent = true };

            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: properties, body: body);

            _logger.LogInformation("Targeted publish {EventType} to {Queue}", type.Name, queueName);
        }
        else
        {
            throw new InvalidOperationException($"EventDeliveryMode {attr?.Mode} is not supported for publishing without target.");
        }
    }

    public async Task SendMessageAsync(Message message, string exchangeName = "", string queueName = "", bool persistent = false)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var channel = await GetChannelAsync();

        await EnsureQueueDeclaredAsync(queueName);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = new BasicProperties { Persistent = persistent };

        await channel.BasicPublishAsync(exchange: exchangeName, routingKey: queueName, mandatory: false, basicProperties: properties, body: body);
    }

    public async Task<T?> ReceiveMessageAsync<T>(CancellationToken token)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return default;
        }

        var channel = await GetChannelAsync();
        var queueName = RabbitMQUtils.GetQueueName(typeof(T), _biAppOptions);

        var tcs = new TaskCompletionSource<T?>();

        await EnsureQueueDeclaredAsync(queueName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                _ = tcs.TrySetResult(message);
            }
            catch (Exception ex)
            {
                await HandleMessageErrorAsync(channel, ea, queueName, ex);
                _ = tcs.TrySetException(ex);
            }

            await Task.Yield();
        };

        _ = await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _ = token.Register(() => tcs.TrySetCanceled(token));

        _ = await tcs.Task;

        return tcs.Task.Result;
    }

    public async Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return default;
        }

        var channel = await GetChannelAsync();
        var queueName = RabbitMQUtils.GetQueueName(eventType, _biAppOptions);

        var tcs = new TaskCompletionSource<object?>();

        await EnsureQueueDeclaredAsync(queueName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                _ = tcs.TrySetResult(obj);
            }
            catch (Exception ex)
            {
                await HandleMessageErrorAsync(channel, ea, queueName, ex);
                _ = tcs.TrySetException(ex);
            }
            await Task.Yield();
        };

        _ = await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _ = token.Register(() => tcs.TrySetCanceled(token));

        _ = await tcs.Task;

        return tcs.Task.Result;
    }

    public async Task<Message> ReceiveMessageAsync(string queueName = "")
    {
        if (!_rabbitMQOptions.Enable)
        {
            return null;
        }

        var channel = await GetChannelAsync();

        await EnsureQueueDeclaredAsync(queueName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        Message? receivedMessage = new();

        var tcs = new TaskCompletionSource<Message>();

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();

            receivedMessage = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(body));

            tcs.SetResult(receivedMessage!);
        };

        _ = await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

        _ = await tcs.Task;

        return tcs.Task.Result;
    }

    public async Task StartConsumingAsync(Type eventType, Func<object, Task> callback, string consumerName)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var channel = await GetChannelAsync();

        var attr = eventType.GetCustomAttribute<EventAttribute>();
        var message = eventType.Name.ToSnakeCase();

        string queueName;
        Dictionary<string, object>? queueArguments = null;

        if (attr?.Mode == EventDeliveryMode.Publish)
        {
            queueName = $"{consumerName}/{message}".ToLowerInvariant();

            var exchange = RabbitMQUtils.GetOwner(attr, _biAppOptions);
            await EnsureExchangeDeclaredAsync(exchange, ExchangeType.Direct);

            if (!string.IsNullOrEmpty(_rabbitMQOptions.DeadLetterExchange))
            {
                await EnsureExchangeDeclaredAsync(_rabbitMQOptions.DeadLetterExchange, ExchangeType.Direct);

                queueArguments = new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = _rabbitMQOptions.DeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queueName
                };
            }

            await EnsureQueueDeclaredAsync(queueName, queueArguments);

            await channel.QueueBindAsync(queue: queueName, exchange: exchange, routingKey: message);

            _logger.LogInformation("Bound queue {Queue} to exchange {Exchange} with rk {RoutingKey}", queueName, exchange, message);
        }
        else if (attr?.Mode == EventDeliveryMode.Targeted)
        {
            queueName = RabbitMQUtils.GetQueueName(eventType, _biAppOptions);

            if (!string.IsNullOrEmpty(_rabbitMQOptions.DeadLetterExchange))
            {
                await EnsureExchangeDeclaredAsync(_rabbitMQOptions.DeadLetterExchange, ExchangeType.Direct);

                queueArguments = new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"] = _rabbitMQOptions.DeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queueName
                };
            }

            await EnsureQueueDeclaredAsync(queueName, queueArguments);
        }
        else
        {
            throw new InvalidOperationException($"EventDeliveryMode {attr?.Mode} is not supported for consuming.");
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                _logger.LogInformation("Event received {EventType}", eventType.Name);

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                await callback(obj!);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                await HandleMessageErrorAsync(channel, ea, queueName, ex);

                _logger.LogError(ex, "Error handling {EventType}", eventType.Name);
            }
        };

        _ = await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Started listening to {Queue}", queueName);
    }

    private async Task HandleMessageErrorAsync(IChannel channel, BasicDeliverEventArgs ea, string queueName, Exception ex)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);

        if (retryCount >= _rabbitMQOptions.MaxRetryCount)
        {
            if (!string.IsNullOrEmpty(_rabbitMQOptions.DeadLetterExchange))
            {
                try
                {
                    var properties = new BasicProperties
                    {
                        Persistent = true,
                        Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object>())
                    };

                    properties.Headers[OriginalQueueHeader] = queueName;
                    properties.Headers["x-failure-reason"] = ex.Message;
                    properties.Headers["x-failure-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await channel.BasicPublishAsync(
                        exchange: _rabbitMQOptions.DeadLetterExchange,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: ea.Body.ToArray());

                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                    _logger.LogWarning(
                        "Message sent to DLX after {RetryCount} retries. Queue: {Queue}, Error: {Error}",
                        retryCount, queueName, ex.Message);
                }
                catch (Exception dlxEx)
                {
                    _logger.LogError(dlxEx, "Failed to send message to DLX. Requeuing message.");
                    await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
            }
            else
            {
                // No DLX configured, reject without requeue
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                _logger.LogWarning("Message rejected after {RetryCount} retries (no DLX configured). Queue: {Queue}", retryCount, queueName);
            }
        }
        else
        {
            // Increment retry count and requeue
            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object?>(ea.BasicProperties.Headers ?? new Dictionary<string, object>())
            };

            SetRetryCount(properties, retryCount + 1);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body.ToArray());

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

            _logger.LogInformation("Message requeued (retry {RetryCount}/{MaxRetryCount}). Queue: {Queue}",
                retryCount + 1, _rabbitMQOptions.MaxRetryCount, queueName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger?.LogInformation("RabbitMQClient disposed");

        try
        {
            if (_channel?.IsOpen == true)
            {
                await _channel.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error closing RabbitMQ channel");
        }

        try
        {
            if (_connection?.IsOpen == true)
            {
                await _connection.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error closing RabbitMQ connection");
        }

        _connectionLock?.Dispose();
        _channelLock?.Dispose();
    }

    private async Task<IConnection> GetConnectionAsync()
    {
        if (_connection?.IsOpen == true)
        {
            return _connection;
        }

        await _connectionLock.WaitAsync();

        try
        {
            if (_connection?.IsOpen == true)
            {
                return _connection;
            }

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing existing connection");
                }
            }

            if (_factory == null)
            {
                throw new InvalidOperationException("RabbitMQ is not enabled or factory is not initialized");
            }

            var appName = $"{_biAppOptions.Environment}-{_biAppOptions.Domain}";
            _connection = await _factory.CreateConnectionAsync(appName);

            _connection.ConnectionShutdownAsync += async (sender, args) =>
            {
                _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);
            };

            _logger.LogInformation("RabbitMQ connection established");

            return _connection;
        }
        finally
        {
            _ = _connectionLock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel?.IsOpen == true)
        {
            return _channel;
        }

        await _channelLock.WaitAsync();

        try
        {
            if (_channel?.IsOpen == true)
            {
                return _channel;
            }

            if (_channel != null)
            {
                try
                {
                    await _channel.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing existing channel");
                }
            }

            var connection = await GetConnectionAsync();

            _channel = await connection.CreateChannelAsync();

            _channel.ChannelShutdownAsync += async (sender, args) =>
            {
                _logger.LogWarning("RabbitMQ channel shutdown: {Reason}", args.ReplyText);
            };

            if (!string.IsNullOrEmpty(_rabbitMQOptions.DeadLetterExchange))
            {
                await EnsureExchangeDeclaredAsync(_rabbitMQOptions.DeadLetterExchange, ExchangeType.Direct);
            }

            _logger.LogInformation("RabbitMQ channel created");

            return _channel;
        }
        finally
        {
            _ = _channelLock.Release();
        }
    }

    private async Task EnsureExchangeDeclaredAsync(string exchange, string exchangeType)
    {
        if (_declaredExchanges.ContainsKey(exchange))
        {
            return;
        }

        var channel = await GetChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: exchange, type: exchangeType, durable: true, autoDelete: false);

        _ = _declaredExchanges.TryAdd(exchange, true);

        _logger.LogDebug("Exchange declared: {Exchange}", exchange);
    }

    private async Task EnsureQueueDeclaredAsync(string queueName, Dictionary<string, object>? arguments = null)
    {
        if (_declaredQueues.ContainsKey(queueName))
        {
            return;
        }

        var channel = await GetChannelAsync();

        _ = await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);

        _ = _declaredQueues.TryAdd(queueName, true);

        _logger.LogDebug("Queue declared: {Queue}", queueName);
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue(RetryCountHeader, out var retryCountObj) == true)
        {
            if (retryCountObj is byte[] bytes)
            {
                return int.Parse(Encoding.UTF8.GetString(bytes));
            }
            if (retryCountObj is int count)
            {
                return count;
            }
        }

        return 0;
    }

    private static void SetRetryCount(IBasicProperties properties, int count)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[RetryCountHeader] = count;
    }

    private void EnsureCorrelationContext<T>(T message) where T : IBaseEvent
    {
        if (message.CorrelationContext == null || message.CorrelationContext == CorrelationContext.Empty)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var correlationContextProvider = scope.ServiceProvider.GetRequiredService<ICorrelationContextProvider>();
            var correlationContext = correlationContextProvider.Get() ?? CorrelationContext.Empty;

            message.CorrelationContext = correlationContext;
        }
    }

    private static void EnsureEventTimestamps<T>(T message) where T : IBaseEvent
    {
        var now = DateTime.UtcNow;

        message.Created = DateOnly.FromDateTime(now);
        message.CreatedTime = TimeOnly.FromDateTime(now);
    }
}
