using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static class AfterCommitEventDispatcher
{
    public static async Task FlushAsync(AfterCommitEventScope? scope, IRabbitMQClient? rabbitMQClient, ILogger logger)
    {
        if (scope is null)
        {
            return;
        }

        var events = scope.Drain();

        if (rabbitMQClient is null || events.Count == 0)
        {
            return;
        }

        foreach (var message in events)
        {
            try
            {
                await rabbitMQClient.PublishAsync(message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "After-commit publish failed for {MessageType}", message.GetType().Name);
            }
        }
    }
}