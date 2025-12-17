using BiUM.Core.Common.API;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.HttpClients;

public interface IHttpClientsService
{
    Task<IApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        CancellationToken cancellationToken = default);

    Task<IApiResponse<TType>> CallService<TType>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<IApiResponse<TType>> Get<TType>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<IApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default);

    Task<IApiResponse<TType>> Post<TType>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default);
}