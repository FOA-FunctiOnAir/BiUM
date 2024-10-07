using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Services.Logging.Serilog;

namespace BiUM.Infrastructure.Common.Configs;

public class Specialized
{
    public RedisClientOptions? RedisClientOptions { get; set; }
    public RabbitMQOptions? RabbitMQOptions { get; set; }
    public SerilogOptions? SerilogOptions { get; set; }
}