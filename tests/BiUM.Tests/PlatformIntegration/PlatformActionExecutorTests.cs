using BiUM.Contract.Models.Api;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.PlatformIntegration;
using BiUM.Specialized.Services.PlatformIntegration;
using FluentAssertions;
using Moq;
using Xunit;

namespace BiUM.Tests.PlatformIntegration;

public sealed class PlatformActionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_service_action_calls_catalog_service()
    {
        var serviceId = Guid.NewGuid();
        var http = new Mock<IHttpClientsService>();
        http.Setup(h => h.CallService(
                serviceId,
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse());

        var executor = CreateExecutor(http.Object);

        var result = await executor.ExecuteAsync(new PlatformActionRequest
        {
            ActionType = Ids.Parameter.EventActionType.Values.Service,
            ServiceId = serviceId,
            Parameters = new Dictionary<string, object?> { ["id"] = Guid.NewGuid() }
        });

        result.Success.Should().BeTrue();
        http.Verify(h => h.CallService(
            serviceId,
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_publish_internal_rabbitmq_publishes_platform_event()
    {
        var eventId = Guid.NewGuid();
        var definition = new EventDefinitionDto
        {
            Id = eventId,
            ApplicationId = Guid.NewGuid(),
            Code = "sample-event",
            ChannelType = Ids.Parameter.EventChannelType.Values.InternalRabbitMq,
            DirectionType = Ids.Parameter.EventDirectionType.Values.Outbound
        };

        var provider = new Mock<IEventDefinitionProvider>();
        provider.Setup(p => p.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        PlatformIntegrationEvent? published = null;
        var rabbit = new Mock<IRabbitMQClient>();
        rabbit.Setup(r => r.PublishAsync(It.IsAny<PlatformIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IBaseEvent, CancellationToken>((message, _) => published = (PlatformIntegrationEvent)message)
            .Returns(Task.CompletedTask);

        var executor = CreateExecutor(
            Mock.Of<IHttpClientsService>(),
            provider.Object,
            rabbit.Object);

        var result = await executor.ExecuteAsync(new PlatformActionRequest
        {
            ActionType = Ids.Parameter.EventActionType.Values.PublishEvent,
            EventId = eventId,
            Parameters = new Dictionary<string, object?> { ["note"] = "hello" }
        });

        result.Success.Should().BeTrue();
        published.Should().NotBeNull();
        published!.EventId.Should().Be(eventId);
        published.EventCode.Should().Be("sample-event");
        published.Parameters["note"].Should().Be("hello");
    }

    [Fact]
    public async Task ExecuteAsync_invoke_event_runs_nested_actions_in_sort_order()
    {
        var rootEventId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var definition = new EventDefinitionDto
        {
            Id = rootEventId,
            ApplicationId = Guid.NewGuid(),
            Code = "invoke-root",
            ChannelType = Ids.Parameter.EventChannelType.Values.InternalRabbitMq,
            DirectionType = Ids.Parameter.EventDirectionType.Values.Outbound,
            Actions =
            [
                new EventActionDefinitionDto
                {
                    ActionType = Ids.Parameter.EventActionType.Values.Service,
                    SortOrder = 1,
                    ServiceId = serviceId
                }
            ]
        };

        var provider = new Mock<IEventDefinitionProvider>();
        provider.Setup(p => p.GetByIdAsync(rootEventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        var http = new Mock<IHttpClientsService>();
        http.Setup(h => h.CallService(
                serviceId,
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse());

        var executor = CreateExecutor(http.Object, provider.Object);

        var result = await executor.ExecuteAsync(new PlatformActionRequest
        {
            ActionType = Ids.Parameter.EventActionType.Values.InvokeEvent,
            EventId = rootEventId
        });

        result.Success.Should().BeTrue();
        http.Verify(h => h.CallService(
            serviceId,
            It.IsAny<Dictionary<string, dynamic>>(),
            It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<IReadOnlyList<Guid>>(),
            It.IsAny<string?>(),
            It.IsAny<int?>(),
            It.IsAny<int?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_records_success_metric()
    {
        Guid? recordedStatus = null;
        var recorder = new Mock<IEventIntegrationMetricRecorder>();
        recorder.Setup(r => r.RecordAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<Guid?, string?, Guid, int, bool, string?, CancellationToken>((_, _, status, _, _, _, _) =>
                recordedStatus = status)
            .Returns(Task.CompletedTask);

        var http = new Mock<IHttpClientsService>();
        http.Setup(h => h.CallService(
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, dynamic>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<IReadOnlyList<Guid>>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse());

        var executor = CreateExecutor(
            http.Object,
            Mock.Of<IEventDefinitionProvider>(),
            Mock.Of<IRabbitMQClient>(),
            recorder.Object);

        await executor.ExecuteAsync(new PlatformActionRequest
        {
            ActionType = Ids.Parameter.EventActionType.Values.Service,
            ServiceId = Guid.NewGuid()
        });

        recordedStatus.Should().Be(Ids.Parameter.EventIntegrationStatusType.Values.Success);
    }

    private static PlatformActionExecutor CreateExecutor(
        IHttpClientsService httpClientsService,
        IEventDefinitionProvider? eventDefinitionProvider = null,
        IRabbitMQClient? rabbitMqClient = null,
        IEventIntegrationMetricRecorder? metricRecorder = null)
    {
        eventDefinitionProvider ??= Mock.Of<IEventDefinitionProvider>();
        metricRecorder ??= Mock.Of<IEventIntegrationMetricRecorder>();

        IEnumerable<IRabbitMQClient> clients = rabbitMqClient is null ? [] : [rabbitMqClient];

        return new PlatformActionExecutor(
            httpClientsService,
            eventDefinitionProvider,
            metricRecorder,
            clients);
    }
}