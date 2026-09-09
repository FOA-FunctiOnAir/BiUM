using BiUM.Core.Authorization;
using BiUM.Core.Compensation;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.Compensation;

public sealed class CompensationSessionFinalizedPublisher : ICompensationSessionFinalizedPublisher
{
    private readonly IRabbitMQClient _rabbitMQClient;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ILogger<CompensationSessionFinalizedPublisher> _logger;

    public CompensationSessionFinalizedPublisher(
        IRabbitMQClient rabbitMQClient,
        ICorrelationContextAccessor correlationContextAccessor,
        ILogger<CompensationSessionFinalizedPublisher> logger)
    {
        _rabbitMQClient = rabbitMQClient;
        _correlationContextAccessor = correlationContextAccessor;
        _logger = logger;
    }

    public Task PublishAsync(Guid compensationSessionId, bool success, CancellationToken cancellationToken = default)
    {
        var ctx = _correlationContextAccessor.CorrelationContext;
        var correlationId = ctx?.CorrelationId ?? Guid.Empty;

        if (ctx is null || correlationId == Guid.Empty)
        {
            _logger.LogWarning(
                "CompensationSessionFinalizedEvent about to publish with empty ambient CorrelationContext for session {SessionId}. ContextIsNull={ContextIsNull}",
                compensationSessionId,
                ctx is null);
        }

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