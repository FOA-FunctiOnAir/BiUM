using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Services.PlatformIntegration;
using FluentAssertions;
using Moq;
using Xunit;

namespace BiUM.Tests.PlatformIntegration;

public sealed class HttpEventIntegrationMetricRecorderTests
{
    [Fact]
    public async Task RecordAsync_posts_observability_metric_endpoint()
    {
        Dictionary<string, dynamic>? captured = null;
        var http = new Mock<IHttpClientsService>();
        http.Setup(h => h.Post(
                "/api/observability/EventIntegrationMetric/RecordEventIntegrationMetric",
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Dictionary<string, dynamic>?, bool, CancellationToken>((_, parameters, _, _) =>
                captured = parameters)
            .ReturnsAsync(new ApiResponse());

        var recorder = new HttpEventIntegrationMetricRecorder(http.Object);
        var eventId = Guid.NewGuid();

        await recorder.RecordAsync(
            eventId,
            "order-created",
            Ids.Parameter.EventIntegrationStatusType.Values.Success,
            42,
            true,
            null,
            CancellationToken.None);

        captured.Should().NotBeNull();
        ((Guid)captured!["eventId"]).Should().Be(eventId);
        ((string)captured["eventCode"]).Should().Be("order-created");
        ((Guid)captured["statusType"]).Should().Be(Ids.Parameter.EventIntegrationStatusType.Values.Success);
        ((int)captured["durationMs"]).Should().Be(42);
        ((bool)captured["success"]).Should().BeTrue();
    }
}