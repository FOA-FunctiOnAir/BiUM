using BiUM.Core.MessageBroker;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.PlatformIntegration;
using BiUM.Infrastructure.Services.MessageBroker.RabbitMQ;
using BiUM.Specialized.Services.PlatformIntegration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BiUM.Tests.PlatformIntegration;

public sealed class PlatformIntegrationEventDispatcherTests
{
    private sealed class RecordingHandler : IPlatformEventHandler
    {
        public RecordingHandler(string eventCode)
        {
            EventCode = eventCode;
        }

        public string EventCode { get; }

        public List<PlatformIntegrationEvent> Received { get; } = [];

        public Task HandleAsync(PlatformIntegrationEvent @event, CancellationToken cancellationToken)
        {
            Received.Add(@event);

            return Task.CompletedTask;
        }
    }

    private sealed class TestDispatcher : PlatformIntegrationEventDispatcher
    {
        public TestDispatcher(IEnumerable<IPlatformEventHandler> handlers) : base(handlers, NullLogger.Instance)
        {
        }
    }

    public sealed class ScannedPlatformEventHandler : IPlatformEventHandler
    {
        public string EventCode => "scanned-event";

        public Task HandleAsync(PlatformIntegrationEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task HandleAsync_routes_event_by_code_case_insensitively()
    {
        var tick = new RecordingHandler("energytracking-collection-tick");
        var other = new RecordingHandler("other-event");

        var dispatcher = new TestDispatcher([tick, other]);

        await dispatcher.HandleAsync(new PlatformIntegrationEvent { EventCode = "EnergyTracking-Collection-Tick" });

        tick.Received.Should().ContainSingle();
        other.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_invokes_every_handler_registered_for_the_code()
    {
        var first = new RecordingHandler("shared");
        var second = new RecordingHandler("shared");

        await new TestDispatcher([first, second]).HandleAsync(new PlatformIntegrationEvent { EventCode = "shared" });

        first.Received.Should().ContainSingle();
        second.Received.Should().ContainSingle();
    }

    [Fact]
    public async Task HandleAsync_ignores_unknown_codes()
    {
        var handler = new RecordingHandler("known");

        var act = () => new TestDispatcher([handler]).HandleAsync(new PlatformIntegrationEvent { EventCode = "unknown" });

        await act.Should().NotThrowAsync();
        handler.Received.Should().BeEmpty();
    }

    [Fact]
    public void AddPlatformEventHandlers_registers_concrete_handlers_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddPlatformEventHandlers(typeof(PlatformIntegrationEventDispatcherTests).Assembly);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPlatformEventHandler) &&
            descriptor.ImplementationType == typeof(ScannedPlatformEventHandler) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Abstract_dispatcher_is_not_subscribed_but_concrete_subclass_is()
    {
        var handlers = RabbitMQUtils.GetAllHandlers().ToList();

        handlers.Should().NotContain(handler => handler.Implementation == typeof(PlatformIntegrationEventDispatcher));
        handlers.Should().Contain(handler => handler.Implementation == typeof(TestDispatcher) && handler.Event == typeof(PlatformIntegrationEvent));
    }

    [Fact]
    public void EventAttribute_consumer_concurrency_defaults_to_one()
    {
        new EventAttribute().ConsumerConcurrency.Should().Be(1);
        new EventAttribute { Exchange = "x", ConsumerConcurrency = 16 }.ConsumerConcurrency.Should().Be(16);
    }
}