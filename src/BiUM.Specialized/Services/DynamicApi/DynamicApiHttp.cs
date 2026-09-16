using BiUM.Contract.Models.Api;
using BiUM.Core.HttpClients;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.DynamicApi;

public sealed class DynamicApiHttp : IDynamicApiHttp
{
    private readonly IHttpClientsService _httpClientsService;

    public DynamicApiHttp(IHttpClientsService httpClientsService)
    {
        _httpClientsService = httpClientsService;
    }

    public Task<ApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        IReadOnlyList<Guid>? selectedIds = null,
        IReadOnlyList<Guid>? excludedIds = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.CallService(
            serviceId,
            parameters,
            selectedIds,
            excludedIds,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<ApiResponse<TResponse>> CallService<TResponse>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        IReadOnlyList<Guid>? selectedIds = null,
        IReadOnlyList<Guid>? excludedIds = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.CallService<TResponse>(
            serviceId,
            parameters,
            selectedIds,
            excludedIds,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<PaginatedApiResponse<TResponse>> CallPaginatedService<TResponse>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        IReadOnlyList<Guid>? selectedIds = null,
        IReadOnlyList<Guid>? excludedIds = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.CallPaginatedService<TResponse>(
            serviceId,
            parameters,
            selectedIds,
            excludedIds,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<ApiResponse<TResponse>> Get<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.Get<TResponse>(
            url,
            parameters,
            external,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<PaginatedApiResponse<TResponse>> GetPaginated<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.GetPaginated<TResponse>(
            url,
            parameters,
            external,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<ApiResponse<string>> GetContent(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.GetContent(
            url,
            parameters,
            external,
            q,
            pageStart,
            pageSize,
            cancellationToken);

    public Task<ApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.Post(url, parameters, external, cancellationToken);

    public Task<ApiResponse<TResponse>> Post<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default) =>
        _httpClientsService.Post<TResponse>(url, parameters, external, cancellationToken);
}