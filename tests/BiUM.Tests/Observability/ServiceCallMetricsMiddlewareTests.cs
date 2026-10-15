using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Observability;
using BiUM.Infrastructure.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BiUM.Tests.Observability;

public sealed class ServiceCallMetricsMiddlewareTests
{
    private readonly Mock<IRabbitMQClient> _rabbitMQClient = new();

    private ServiceCallMetricsMiddleware CreateMiddleware(params object[] endpointMetadata)
    {
        RequestDelegate next = context =>
        {
            context.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(endpointMetadata), "test"));
            context.Response.StatusCode = StatusCodes.Status200OK;

            return Task.CompletedTask;
        };

        return new ServiceCallMetricsMiddleware(
            next,
            _rabbitMQClient.Object,
            Options.Create(new BiAppOptions { Environment = "Test", Domain = "Observability" }),
            NullLogger<ServiceCallMetricsMiddleware>.Instance);
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();

        context.Request.Path = path;
        context.Request.Method = HttpMethods.Post;

        return context;
    }

    [Fact]
    public async Task Publishes_service_called_event_for_regular_endpoints()
    {
        ServiceCalledEvent? published = null;

        _rabbitMQClient
            .Setup(client => client.PublishAsync(It.IsAny<ServiceCalledEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ServiceCalledEvent, CancellationToken>((message, _) => published = message)
            .Returns(Task.CompletedTask);

        await CreateMiddleware().InvokeAsync(CreateContext("/api/observability/ServiceCallMetric/GetServiceCallMetrics"), Mock.Of<ICorrelationContextProvider>());

        published.Should().NotBeNull();
        published!.ServiceName.Should().Be("BiApp.Observability");
        published.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Skips_endpoints_marked_with_disabled_service_call_metric()
    {
        await CreateMiddleware(new ServiceCallMetricAttribute(false))
            .InvokeAsync(CreateContext("/api/observability/EventIntegrationMetric/RecordEventIntegrationMetric"), Mock.Of<ICorrelationContextProvider>());

        _rabbitMQClient.Verify(client => client.PublishAsync(It.IsAny<ServiceCalledEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Most_specific_metadata_wins_when_action_re_enables_metric()
    {
        await CreateMiddleware(new ServiceCallMetricAttribute(false), new ServiceCallMetricAttribute(true))
            .InvokeAsync(CreateContext("/api/observability/EventIntegrationMetric/Other"), Mock.Of<ICorrelationContextProvider>());

        _rabbitMQClient.Verify(client => client.PublishAsync(It.IsAny<ServiceCalledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IsMetricDisabled_is_false_without_endpoint()
    {
        ServiceCallMetricsMiddleware.IsMetricDisabled(new DefaultHttpContext()).Should().BeFalse();
    }
}