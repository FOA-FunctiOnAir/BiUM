using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Specialized.Common.API;

namespace BiUM.Test2.API.Controllers;

[BiUMRoute("test")]
public class TestRabbitMQController : ApiControllerBase
{
    private readonly IRabbitMQClient _rabbitMQClient;

    public TestRabbitMQController(IRabbitMQClient rabbitMQClient)
    {
        _rabbitMQClient = rabbitMQClient;
    }
}
