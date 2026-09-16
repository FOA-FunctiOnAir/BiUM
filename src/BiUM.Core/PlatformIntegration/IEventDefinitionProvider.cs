using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.PlatformIntegration;

public interface IEventDefinitionProvider
{
    Task<EventDefinitionDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    Task<EventDefinitionDto?> GetByCodeAsync(string eventCode, CancellationToken cancellationToken = default);
}