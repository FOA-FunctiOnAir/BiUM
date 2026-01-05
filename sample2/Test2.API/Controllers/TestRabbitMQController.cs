using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
