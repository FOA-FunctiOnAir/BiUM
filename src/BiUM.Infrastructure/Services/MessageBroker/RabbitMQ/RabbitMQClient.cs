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

public partial class RabbitMQClient : IRabbitMQClient
{
    private readonly string _queueTemplate = "{{owner}}/{{message}}";
    private readonly RabbitMQOptions _rabbitMQOptions;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
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
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public RabbitMQClient(IOptions<RabbitMQOptions> rabbitMQOptions, ILogger<RabbitMQClient> logger)
    {
        _rabbitMQOptions = rabbitMQOptions.Value;
        _logger = logger;

        if (!_rabbitMQOptions.Enable)
        {
            return;
        }

        var factory = new ConnectionFactory
        {
            Uri = new Uri($"amqp://{_rabbitMQOptions.UserName}:{_rabbitMQOptions.Password}@{_rabbitMQOptions.Hostname}:{_rabbitMQOptions.Port}/{_rabbitMQOptions.VirtualHost}"),
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync<T>(T message)
    {
        var queueName = GetQueueName(typeof(T));

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(string target, T message)
    {
        var queueName = GetQueueName(typeof(T), target);

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;

        _channel.BasicPublish(
            exchange: "",
            routingKey: queueName,
            basicProperties: properties,
            body: body);

        Console.WriteLine("PublishAsync sent");

        return Task.CompletedTask;
    }

    public void SendMessage(Message message, string exchangeName = "", string queueName = "", bool persistent = false)
    {
        // Declare a queue
        _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        // Convert the message to bytes
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = persistent;

        // Publish the message to the queue
        _channel.BasicPublish(exchange: exchangeName, routingKey: queueName, basicProperties: properties, body: body);
    }

    public Task<T?> ReceiveMessageAsync<T>(CancellationToken token)
    {
        var queueName = GetQueueName(typeof(T));

        var tcs = new TaskCompletionSource<T?>();

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<T>(json);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                tcs.TrySetResult(message);
            }
            catch (Exception ex)
            {
                // Hata durumunda reject et
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                tcs.TrySetException(ex);
            }

            await Task.Yield();
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        // Cancellation destekle
        token.Register(() => tcs.TrySetCanceled(token));

        return tcs.Task;
    }

    public Task<object?> ReceiveMessageAsync(Type eventType, CancellationToken token)
    {
        var queueName = GetQueueName(eventType);

        var tcs = new TaskCompletionSource<object?>();

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
                tcs.TrySetResult(obj);
            }
            catch (Exception ex)
            {
                _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                tcs.TrySetException(ex);
            }
            await Task.Yield();
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        token.Register(() => tcs.TrySetCanceled(token));

        return tcs.Task;
    }

    public async Task<Message> ReceiveMessageAsync(string queueName = "")
    {
        // Declare the queue to consume from
        _channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(_channel);
        Message? receivedMessage = new();

        var tcs = new TaskCompletionSource<Message>();

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            receivedMessage = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(body));
            tcs.SetResult(receivedMessage!);
        };

        _channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

        await tcs.Task; // Wait for the message to be received before returning

        return tcs.Task.Result;
    }

    public void StartConsuming(Type eventType, Func<object, Task> callback)
    {
        var queueName = GetQueueName(eventType);

        _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var obj = JsonSerializer.Deserialize(json, eventType);

                await callback(obj);

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _channel.BasicNack(ea.DeliveryTag, false, true);

                _logger.LogError(ex, "Error handling {EventType}", eventType.Name);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

        _logger.LogInformation("Started listening to {Queue}", queueName);
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
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