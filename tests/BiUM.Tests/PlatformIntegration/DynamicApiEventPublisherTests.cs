using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Core.PlatformIntegration;
using BiUM.Specialized.Services.DynamicApi;
using FluentAssertions;
using Moq;
using Xunit;

namespace BiUM.Tests.PlatformIntegration;

public sealed class DynamicApiEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_by_id_delegates_to_platform_executor()
    {
        var eventId = Guid.NewGuid();
        PlatformActionRequest? captured = null;

        var executor = new Mock<IPlatformActionExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<PlatformActionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PlatformActionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new ApiResponse());

        var publisher = new DynamicApiEventPublisher(executor.Object);

        await publisher.PublishAsync(eventId, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ActionType.Should().Be(Ids.Parameter.EventActionType.Values.PublishEvent);
        captured.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task PublishAsync_by_code_delegates_to_platform_executor()
    {
        PlatformActionRequest? captured = null;

        var executor = new Mock<IPlatformActionExecutor>();
        executor.Setup(e => e.ExecuteAsync(It.IsAny<PlatformActionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PlatformActionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(new ApiResponse());

        var publisher = new DynamicApiEventPublisher(executor.Object);

        await publisher.PublishAsync("order-created", CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.ActionType.Should().Be(Ids.Parameter.EventActionType.Values.PublishEvent);
        captured.EventCode.Should().Be("order-created");
    }
}