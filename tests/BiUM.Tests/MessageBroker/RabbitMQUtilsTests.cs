using BiUM.Core.MessageBroker.Events;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.Specialized.Services.Compensation;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.MessageBroker;

public sealed class RabbitMQUtilsTests
{
    [Fact]
    public void GetAllHandlers_includes_compensation_session_finalized_handler()
    {
        _ = typeof(CompensationSessionFinalizedHandler);

        var handlers = RabbitMQUtils.GetAllHandlers().ToList();

        handlers.Should().Contain(t =>
            t.Event == typeof(CompensationSessionFinalizedEvent)
            && t.Implementation == typeof(CompensationSessionFinalizedHandler));
    }
}
