using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Models.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Common.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public class RabbitMQClient : IRabbitMQClient
{
    private readonly string _queueTemplate = "{{owner}}/{{message}}";
    private readonly BiAppOptions _biAppOptions;
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private readonly ILogger<RabbitMQClient> _logger;

    public RabbitMQClient(RabbitMQOptions options)
    {
        _rabbitMQOptions = options;

        if (!_rabbitMQOptions.Enable)
        {
            // throw new InvalidOperationException("RabbitMQ is not enabled.");

            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{options.UserName}:{options.Password}@{options.Hostname}:{options.Port}/{options.VirtualHost}")
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public RabbitMQClient(IOptions<BiAppOptions> biAppOptions, IOptions<RabbitMQOptions> rabbitMQOptions, ILogger<RabbitMQClient> logger)
    {
        _biAppOptions = biAppOptions.Value;
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;

        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{_rabbitMQOptions.UserName}:{_rabbitMQOptions.Password}@{_rabbitMQOptions.Hostname}:{_rabbitMQOptions.Port}/{_rabbitMQOptions.VirtualHost}")
        };

        var appName = $"{_biAppOptions.Environment}-{_biAppOptions.Domain}";

        _connection = factory.CreateConnectionAsync(appName).GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        var queueName = GetQueueName(typeof(T));

        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties { Persistent = true };

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    public async Task PublishAsync<T>(string target, T message)
    {
        ArgumentNullException.ThrowIfNull(_connection);
        ArgumentNullException.ThrowIfNull(_channel);

        var queueName = GetQueueName(typeof(T), target);

        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        _logger.LogInformation("Connection open: {Open}, Channel open: {ChOpen}", _connection.IsOpen, _channel.IsOpen);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties { Persistent = true };

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body);

        _logger.LogInformation("PublishAsync sent");
    }

    public async Task SendMessageAsync(Message message, string exchangeName = "", string queueName = "", bool persistent = false)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        // Declare a queue
        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        // Convert the message to bytes
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

        var properties = new BasicProperties { Persistent = true };

        // Publish the message to the queue
        await _channel.BasicPublishAsync(exchange: exchangeName, routingKey: queueName, mandatory: false, basicProperties: properties, body: body);
    }

    // TODO: Logic gözden geçirilmeli
    public async Task<T?> ReceiveMessageAsync<T>(CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        var queueName = GetQueueName(typeof(T));

        var tcs = new TaskCompletionSource<T?>();

        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                tcs.TrySetResult(message);
            }
            catch (Exception ex)
            {
                // Hata durumunda reject et
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);

                tcs.TrySetException(ex);
            }

            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        // Cancellation destekle
        token.Register(() => tcs.TrySetCanceled(token));

        await tcs.Task;

        return tcs.Task.Result;
    }

    // TODO: Logic gözden geçirilmeli
    public async Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        var queueName = GetQueueName(eventType);

        var tcs = new TaskCompletionSource<object?>();

        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                tcs.TrySetResult(obj);
            }
            catch (Exception ex)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);

                tcs.TrySetException(ex);
            }
            await Task.Yield();
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        token.Register(() => tcs.TrySetCanceled(token));

        await tcs.Task;

        return tcs.Task.Result;
    }

    // TODO: Logic gözden geçirilmeli
    public async Task<Message> ReceiveMessageAsync(string queueName = "")
    {
        ArgumentNullException.ThrowIfNull(_channel);

        // Declare the queue to consume from
        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        Message? receivedMessage = new();

        var tcs = new TaskCompletionSource<Message>();

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();

            receivedMessage = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(body));

            tcs.SetResult(receivedMessage!);
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

        await tcs.Task; // Wait for the message to be received before returning

        return tcs.Task.Result;
    }

    public async Task StartConsumingAsync(Type eventType, Func<object, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(_channel);

        var queueName = GetQueueName(eventType);

        await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                _logger.LogInformation("Event received {EventType}", eventType.Name);

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                await callback(obj);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);

                _logger.LogError(ex, "Error handling {EventType}", eventType.Name);
            }
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Started listening to {Queue}", queueName);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("RabbitMQClient disposed");

        await _channel?.CloseAsync();
        await _connection?.CloseAsync();
    }

    private string GetQueueName(Type type, string? target = null)
    {
        var attribute = type.GetCustomAttribute<EventAttribute>();
        var message = SnakeCase(type.Name);

        var owner = attribute?.Owner ?? "BiApp.Error";

        if (!string.IsNullOrEmpty(attribute?.Target))
        {
            owner += "__" + attribute.Target;
        }
        else if (!string.IsNullOrEmpty(target))
        {
            owner += "__" + target;
        }

        var queue =
            _queueTemplate
                .Replace("{{owner}}", owner)
                .Replace("{{message}}", message);

        return queue;
    }

    private static string SnakeCase(string value)
        => string.Concat(value.Select((x, i) =>
                i > 0 && value[i - 1] != '.' && value[i - 1] != '/' && char.IsUpper(x) ? "_" + x : x.ToString()))
            .ToLowerInvariant();
}