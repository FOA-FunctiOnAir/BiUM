using BiUM.Core.MessageBroker.Events;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.PlatformIntegration;

public interface IPlatformEventHandler
{
    string EventCode { get; }

    Task HandleAsync(PlatformIntegrationEvent @event, CancellationToken cancellationToken);
}