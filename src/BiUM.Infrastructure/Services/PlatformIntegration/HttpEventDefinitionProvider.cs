using BiUM.Core.HttpClients;
using BiUM.Core.PlatformIntegration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.PlatformIntegration;

public sealed class HttpEventDefinitionProvider : IEventDefinitionProvider
{
    private readonly IHttpClientsService _httpClientsService;

    public HttpEventDefinitionProvider(IHttpClientsService httpClientsService)
    {
        _httpClientsService = httpClientsService;
    }

    public Task<EventDefinitionDto?> GetByIdAsync(Guid eventId, CancellationToken cancellationToken = default) =>
        GetInternalAsync(new Dictionary<string, dynamic> { ["id"] = eventId }, cancellationToken);

    public Task<EventDefinitionDto?> GetByCodeAsync(string eventCode, CancellationToken cancellationToken = default) =>
        GetInternalAsync(new Dictionary<string, dynamic> { ["code"] = eventCode }, cancellationToken);

    private async Task<EventDefinitionDto?> GetInternalAsync(
        Dictionary<string, dynamic> parameters,
        CancellationToken cancellationToken)
    {
        var response = await _httpClientsService.Get<EventDefinitionDto>(
            "/api/configuration/Event/GetFwEventDefinition",
            parameters,
            cancellationToken: cancellationToken);

        return response.Success ? response.Value : null;
    }
}