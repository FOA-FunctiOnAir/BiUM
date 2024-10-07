using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Models.MessageBroker.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public partial class RabbitMQClient : IRabbitMQClient
{
    private readonly RabbitMQOptions _options;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;

    public RabbitMQClient()
    {
    }

    public RabbitMQClient(RabbitMQOptions options)
    {
        _options = options;

        if (!_options.Enable)
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

    public void Dispose()
    {
        _channel.Dispose();
    }
}