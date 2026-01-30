using BiUM.Contract.Models.Api;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Core.HttpClients;

public interface IHttpClientsService
{
    Task<ApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TResponse>> CallService<TResponse>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TResponse>> Get<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<TResponse>> Post<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default);
}