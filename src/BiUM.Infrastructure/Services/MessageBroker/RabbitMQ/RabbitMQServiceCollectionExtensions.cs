using Microsoft.Extensions.DependencyInjection;

namespace BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQEventHandlers(this IServiceCollection services)
    {
        foreach (var handler in RabbitMQUtils.GetAllHandlers())
        {
            services.AddScoped(handler.Interface, handler.Implementation);
        }

        return services;
    }
}
