using BiUM.Core.MessageBroker.Events;
using BiUM.Specialized.Services.Compensation;
using Moq;
using Xunit;

namespace BiUM.Tests.Compensation;

public sealed class CompensationSessionFinalizedHandlerTests
{
    [Fact]
    public async Task Success_true_calls_CommitSessionAsync()
    {
        var id = Guid.NewGuid();
        var compensation = new Mock<ICompensationService>();
        var handler = new CompensationSessionFinalizedHandler(compensation.Object);

        await handler.HandleAsync(
            new CompensationSessionFinalizedEvent
            {
                CompensationSessionId = id,
                Success = true
            },
            CancellationToken.None);

        compensation.Verify(c => c.CommitSessionAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        compensation.Verify(c => c.RollbackSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Success_false_calls_RollbackSessionAsync()
    {
        var id = Guid.NewGuid();
        var compensation = new Mock<ICompensationService>();
        var handler = new CompensationSessionFinalizedHandler(compensation.Object);

        await handler.HandleAsync(
            new CompensationSessionFinalizedEvent
            {
                CompensationSessionId = id,
                Success = false
            },
            CancellationToken.None);

        compensation.Verify(c => c.RollbackSessionAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        compensation.Verify(c => c.CommitSessionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}