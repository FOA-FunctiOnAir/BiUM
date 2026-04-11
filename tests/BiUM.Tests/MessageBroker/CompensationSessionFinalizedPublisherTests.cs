using BiUM.Contract.Models;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Infrastructure.Services.Compensation;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace BiUM.Tests.MessageBroker;

public sealed class CompensationSessionFinalizedPublisherTests
{
    [Fact]
    public async Task PublishAsync_sends_event_with_session_and_success_flag()
    {
        var sessionId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var rabbit = new Mock<IRabbitMQClient>();
        CompensationSessionFinalizedEvent? captured = null;

        rabbit
            .Setup(r => r.PublishAsync(It.IsAny<CompensationSessionFinalizedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CompensationSessionFinalizedEvent, CancellationToken>((e, _) => captured = e)
            .Returns(Task.CompletedTask);

        var accessor = new TestCorrelationContextAccessor
        {
            CorrelationContext = new CorrelationContext { CorrelationId = correlationId }
        };

        var publisher = new CompensationSessionFinalizedPublisher(rabbit.Object, accessor);

        await publisher.PublishAsync(sessionId, success: true, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CompensationSessionId.Should().Be(sessionId);
        captured.Success.Should().BeTrue();
        captured.CorrelationId.Should().Be(correlationId);

        rabbit.Verify(
            r => r.PublishAsync(It.IsAny<CompensationSessionFinalizedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
