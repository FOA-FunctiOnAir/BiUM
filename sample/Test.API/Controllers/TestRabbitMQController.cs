using BiApp.Test.Application.Features.Currencies.Events.TestAdded;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class TestRabbitMQController : ApiControllerBase
{
    private readonly IRabbitMQClient _rabbitMQClient;

    public TestRabbitMQController(IRabbitMQClient rabbitMQClient)
    {
        _rabbitMQClient = rabbitMQClient;
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> TriggerEvent(string key)
    {
        var testAddedEvent = new TestAddedEvent
        {
            Key = key
        };

        await _rabbitMQClient.PublishAsync(testAddedEvent);

        return Ok($"Key '{key}' published.");
    }
}
