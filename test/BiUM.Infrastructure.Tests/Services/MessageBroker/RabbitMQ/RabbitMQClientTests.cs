using NUnit.Framework;
using System;
using System.Threading.Tasks;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.Core.Models.MessageBroker.RabbitMQ;
using RabbitMQ.Client;
using BiUM.Core.Common.Configs;

namespace BiUM.Infrastructure.Tests.Services.MessageBroker.RabbitMQ;

public class RabbitMQClientTests
{
    //private IRabbitMQClient? _rabbitMQClient;

    //[SetUp]
    //public void Setup()
    //{
    //    // Replace with your own configuration values
    //    var options = new RabbitMQOptions
    //    {
    //        Enable = true,
    //        //// Azure
    //        Hostname = "38.242.253.6",
    //        Port = 30673,
    //        VirtualHost = "",
    //        //VirtualHost = "finance", // If you want to add virtualHost you need to add only the name 'finance'
    //        UserName = "myuser",
    //        Password = "mypassword"

    //        //// Local Host
    //        //Hostname = "localhost",
    //        //Port = 5672,
    //        //VirtualHost = "",
    //        //VirtualHost = "finance", // If you want to add virtualHost you need to add only the name 'finance'
    //        //UserName = "guest",
    //        //Password = "guest"
    //    };

    //    _rabbitMQClient = new RabbitMQClient(options);
    //}

    //[Test]
    //public void SendMessageAsync_WithValidMessage_ShouldSucceed()
    //{
    //    // Arrange
    //    var message = new Message
    //    {
    //        Title = "Test Message",
    //        Body = "This is a test message.",
    //        Timestamp = DateTime.UtcNow
    //    };

    //    // Act
    //    _rabbitMQClient?.SendMessage(message);

    //    // Assert
    //    // No exceptions should have been thrown
    //}

    //[Test]
    //public async Task ReceiveMessageAsync_WithValidMessage_ShouldReturnMessage()
    //{
    //    // Arrange
    //    var message = new Message
    //    {
    //        Title = "Test Message",
    //        Body = "This is a test message.",
    //        Timestamp = DateTime.UtcNow
    //    };

    //    string queueId = Guid.NewGuid().ToString();

    //    _rabbitMQClient?.SendMessage(message, queueName: queueId);

    //    // Act
    //    var receivedMessage = await _rabbitMQClient!.ReceiveMessageAsync(queueName: queueId);

    //    // Assert
    //    Assert.That(receivedMessage, Is.Not.Null);
    //    Assert.Multiple(() =>
    //    {
    //        Assert.That(receivedMessage.Title, Is.EqualTo(message.Title));
    //        Assert.That(receivedMessage.Body, Is.EqualTo(message.Body));
    //        Assert.That(receivedMessage.Timestamp, Is.EqualTo(message.Timestamp));
    //    });
    //}
}