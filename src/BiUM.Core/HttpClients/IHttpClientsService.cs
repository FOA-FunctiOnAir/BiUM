using BiUM.Core.Common.API;

namespace BiUM.Core.HttpClients;

public interface IHttpClientsService
{
    Task<IApiResponse<TType>> CallService<TType>(
        Guid correlationId,
        Guid serviceId,
        Guid tenantId,
        Guid languageId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<IApiResponse<TType>> Get<TType>(
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<IApiResponse> Post(
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default);

    Task<IApiResponse<TType>> Post<TType>(
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default);
}