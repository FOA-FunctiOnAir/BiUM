using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Consts;
using BiUM.Core.Extensions;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.RabbitMQ;
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
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

internal sealed class RabbitMQClient : IRabbitMQClient, IAsyncDisposable
{
    private const string RetryCountHeader = "x-retry-count";
    private const string OriginalQueueHeader = "x-original-queue";
    private const string DeadLetterExchange = "common.dlx";

    private const string DefaultContentType = "application/x-memorypack";
    private const string DefaultContentEncoding = "brotli";

    private readonly RabbitMQConnectionProvider _connectionProvider;
    private readonly RabbitMQPublisherChannelPool _publisherChannelPool;
    private readonly IRabbitMQSerializer _serializer;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly BiAppOptions _appOptions;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly ILogger<RabbitMQClient> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ConcurrentBag<IChannel> _consumerChannels = [];


    public RabbitMQClient(
        RabbitMQConnectionProvider connectionProvider,
        RabbitMQPublisherChannelPool publisherChannelPool,
        IRabbitMQSerializer serializer,
        ICorrelationContextAccessor correlationContextAccessor,
        IOptions<BiAppOptions> biAppOptions,
        IOptions<RabbitMQOptions> rabbitMQOptions,
        ILogger<RabbitMQClient> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _connectionProvider = connectionProvider;
        _publisherChannelPool = publisherChannelPool;
        _serializer = serializer;
        _correlationContextAccessor = correlationContextAccessor;
        _appOptions = biAppOptions.Value;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : IBaseEvent
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        EnsureEventTimestamps(message);

        var type = typeof(T);

        var eventAttribute = type.GetCustomAttribute<EventAttribute>();

        var correlationContext = _correlationContextAccessor.CorrelationContext ?? CorrelationContext.Empty;

        if (!string.IsNullOrWhiteSpace(eventAttribute?.Exchange)) // Commands: Multiple publishers to single consumer
        {
            var body = await _serializer.SerializeAsync(message, cancellationToken: CancellationToken.None);

            var correlationContextData = await _serializer.SerializeAsync(correlationContext, cancellationToken: CancellationToken.None);

            var properties = new BasicProperties
            {
                AppId = _appOptions.Domain,
                Type = type.Name,
                ContentType = DefaultContentType,
                ContentEncoding = DefaultContentEncoding,
                Persistent = true,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = correlationContext.CorrelationId.ToString("N"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>()
            };

            properties.Headers![HeaderKeys.CorrelationContext] = correlationContextData.ToArray();

            var exchange = eventAttribute.Exchange;
            var messageKey = type.Name.ToSnakeCase();
            var routingKey = $"{exchange}.{messageKey}";

            using var channel = await _publisherChannelPool.GetChannelAsync();

            await channel.Channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.Channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body, cancellationToken: cancellationToken);

            _logger.LogInformation("{MessageType} message published to exchange {Exchange} with routingKey {RoutingKey}",
                type.Name,
                exchange,
                routingKey);
        }
        else // Events: Single publisher to multiple consumers (broadcast)
        {
            var body = await _serializer.SerializeAsync(message, cancellationToken: CancellationToken.None);

            var correlationContextData = await _serializer.SerializeAsync(correlationContext, cancellationToken: CancellationToken.None);

            var properties = new BasicProperties
            {
                AppId = _appOptions.Domain,
                Type = type.Name,
                ContentType = DefaultContentType,
                ContentEncoding = DefaultContentEncoding,
                Persistent = true,
                MessageId = Guid.NewGuid().ToString("N"),
                CorrelationId = correlationContext.CorrelationId.ToString("N"),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Headers = new Dictionary<string, object?>()
            };

            properties.Headers![HeaderKeys.CorrelationContext] = correlationContextData.ToArray();

            var messageKey = type.Name.ToSnakeCase();
            var exchange = $"{_appOptions.Domain.ToLowerInvariant()}.{messageKey}";

            using var channel = await _publisherChannelPool.GetChannelAsync();

            await channel.Channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.Channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("{MessageType} message broadcasted to fanout exchange {Exchange}",
                type.Name,
                exchange);
        }
    }

    public async Task StartConsumingAsync(Type eventType, Type handlerType, CancellationToken cancellationToken)
    {
        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var channel = await CreateConsumerChannelAsync();

        var eventAttribute = eventType.GetCustomAttribute<EventAttribute>();

        Dictionary<string, object?>? queueArguments = null;

        string queueName;

        if (!string.IsNullOrWhiteSpace(eventAttribute?.Exchange) && eventAttribute.Exchange != _appOptions.Domain.ToLowerInvariant()) // Events: Single publisher to multiple consumers (broadcast)
        {
            var messageKey = eventType.Name.ToSnakeCase();
            var exchange = $"{eventAttribute.Exchange}.{messageKey}";

            queueName = $"{_appOptions.Domain.ToLowerInvariant()}.{exchange}";

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            if (_rabbitMQOptions.DeadLetterQueueEnabled)
            {
                queueArguments = new()
                {
                    ["x-dead-letter-exchange"] = DeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queueName
                };
            }

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: exchange,
                routingKey: string.Empty,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Event queue {Queue} created and bound to fanout exchange {Exchange}",
                queueName,
                exchange);
        }
        else // Commands: Multiple publishers to single consumer
        {
            var exchange = _appOptions.Domain.ToLowerInvariant();
            var messageKey = eventType.Name.ToSnakeCase();
            var routingKey = $"{exchange}.{messageKey}";

            queueName = $"{exchange}.{messageKey}";

            await channel.ExchangeDeclareAsync(
                exchange: exchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            if (_rabbitMQOptions.DeadLetterQueueEnabled)
            {
                queueArguments = new()
                {
                    ["x-dead-letter-exchange"] = DeadLetterExchange,
                    ["x-dead-letter-routing-key"] = queueName
                };
            }

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: queueName,
                exchange: exchange,
                routingKey: routingKey,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Owned message queue {Queue} created and bound to owned exchange {Exchange} with routing key {RoutingKey}",
                queueName,
                exchange,
                routingKey);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();

            var scopedServiceProvider = serviceScope.ServiceProvider;

            var cancellationTokenSource = scopedServiceProvider.GetService<CancellationTokenSource>();

            var scopeCancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

            var correlationContextAccessor = scopedServiceProvider.GetService<ICorrelationContextAccessor>();

            try
            {
                _logger.LogInformation("Event received {EventType}", eventType.Name);

                var message = await _serializer.DeserializeAsync(args.Body.ToArray(), eventType, scopeCancellationToken);

                var correlationContextHeader = args.BasicProperties.Headers?[HeaderKeys.CorrelationContext];

                var rawCorrelationContext = correlationContextHeader switch
                {
                    byte[] bytes => bytes,
                    ArraySegment<byte> segment => segment.ToArray(),
                    _ => null
                };

                var correlationContext =
                    rawCorrelationContext is not null
                        ? await _serializer.DeserializeAsync<CorrelationContext>(rawCorrelationContext, scopeCancellationToken)
                        : CorrelationContext.Empty;

                if (correlationContextAccessor is not null)
                {
                    correlationContextAccessor.CorrelationContext = correlationContext;
                }

                dynamic handler = scopedServiceProvider.GetRequiredService(handlerType);

                await handler.HandleAsync(message, scopeCancellationToken);

                await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: scopeCancellationToken);
            }
            catch (Exception ex)
            {
                await HandleMessageErrorAsync(channel, args, queueName, ex, scopeCancellationToken);

                _logger.LogError(ex, "Error handling {EventType}", eventType.Name);
            }
        };

        if (_rabbitMQOptions.DeadLetterQueueEnabled)
        {
            var deadLetterQueueName = $"{queueName}.dlq";

            await channel.ExchangeDeclareAsync(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: deadLetterQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: deadLetterQueueName,
                exchange: DeadLetterExchange,
                routingKey: queueName,
                cancellationToken: cancellationToken);
        }

        await channel.BasicConsumeAsync(
            queue: queueName
            , autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Started listening to {Queue}", queueName);
    }

    private async Task HandleMessageErrorAsync(
        IChannel channel,
        BasicDeliverEventArgs args,
        string queueName,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryCount = GetRetryCount(args.BasicProperties);

        if (retryCount >= _rabbitMQOptions.MaxRetryCount)
        {
            if (_rabbitMQOptions.DeadLetterQueueEnabled)
            {
                try
                {
                    var properties = new BasicProperties(args.BasicProperties)
                    {
                        Persistent = true
                    };

                    properties.Headers ??= new Dictionary<string, object?>();
                    properties.Headers[OriginalQueueHeader] = queueName;
                    properties.Headers["x-failure-reason"] = exception.Message;
                    properties.Headers["x-failure-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                    await channel.BasicPublishAsync(
                        exchange: DeadLetterExchange,
                        routingKey: queueName,
                        mandatory: false,
                        basicProperties: properties,
                        body: args.Body,
                        cancellationToken: cancellationToken);

                    await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

                    _logger.LogWarning(
                        "Message sent to DLX after {RetryCount} retries. Queue: {Queue}, Error: {Error}",
                        retryCount,
                        queueName,
                        exception.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send message to DLX. Requeuing message.");

                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        multiple: false,
                        requeue: true,
                        cancellationToken: cancellationToken);
                }
            }
            else
            {
                // No DLX configured, reject without requeue
                await channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken: cancellationToken);

                _logger.LogWarning("Message rejected after {RetryCount} retries (no DLX configured). Queue: {Queue}",
                    retryCount,
                    queueName);
            }
        }
        else
        {
            var properties = new BasicProperties(args.BasicProperties)
            {
                Persistent = true
            };

            SetRetryCount(properties, retryCount + 1);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: properties,
                body: args.Body, cancellationToken: cancellationToken);

            await channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

            _logger.LogInformation("Message requeued (retry {RetryCount}/{MaxRetryCount}). Queue: {Queue}",
                retryCount + 1,
                _rabbitMQOptions.MaxRetryCount,
                queueName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("RabbitMQClient disposed");

        foreach (var channel in _consumerChannels)
        {
            try
            {
                if (channel.IsOpen)
                {
                    await channel.CloseAsync();
                }

                await channel.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing consumer channel");
            }
        }

        _consumerChannels.Clear();
    }

    private async Task<IChannel> CreateConsumerChannelAsync()
    {
        var connection = await _connectionProvider.GetConsumerConnectionAsync();

        var channel = await connection.CreateChannelAsync();

        channel.ChannelShutdownAsync += (_, args) =>
        {
            _logger.LogWarning("RabbitMQ consumer channel shutdown: {Reason}", args.ReplyText);

            return Task.CompletedTask;
        };

        _consumerChannels.Add(channel);

        return channel;
    }

    private static int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers?.TryGetValue(RetryCountHeader, out var value) != true)
        {
            return 0;
        }

        return value switch
        {
            byte[] bytes => int.Parse(Encoding.UTF8.GetString(bytes)),
            int count => count,
            _ => 0
        };
    }

    private static void SetRetryCount(IBasicProperties properties, int count)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[RetryCountHeader] = count;
    }

    private static void EnsureEventTimestamps<T>(T message) where T : IBaseEvent
    {
        var now = DateTime.UtcNow;

        message.Created = DateOnly.FromDateTime(now);
        message.CreatedTime = TimeOnly.FromDateTime(now);
    }
}
