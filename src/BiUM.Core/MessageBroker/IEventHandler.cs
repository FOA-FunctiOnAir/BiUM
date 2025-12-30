using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.MessageBroker;

public interface IEventHandler<in TEvent> where TEvent : IBaseEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
