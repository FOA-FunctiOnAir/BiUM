using BiUM.Core.Authorization;
using BiUM.Core.Compensation;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.Compensation;

public sealed class CompensationSessionFinalizedPublisher : ICompensationSessionFinalizedPublisher
{
    private readonly IRabbitMQClient _rabbitMQClient;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public CompensationSessionFinalizedPublisher(
        IRabbitMQClient rabbitMQClient,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _rabbitMQClient = rabbitMQClient;
        _correlationContextAccessor = correlationContextAccessor;
    }

    public Task PublishAsync(Guid compensationSessionId, bool success, CancellationToken cancellationToken = default)
    {
        var ctx = _correlationContextAccessor.CorrelationContext;
        var correlationId = ctx?.CorrelationId ?? Guid.Empty;

        var message = new CompensationSessionFinalizedEvent
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            Active = true,
            Deleted = false,
            CompensationSessionId = compensationSessionId,
            Success = success
        };

        return _rabbitMQClient.PublishAsync(message, cancellationToken);
    }
}