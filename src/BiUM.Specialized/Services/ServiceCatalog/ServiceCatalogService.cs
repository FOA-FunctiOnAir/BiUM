using BiUM.Contract.Models.Api;
using BiUM.Contract.Models.MessageBroker;
using BiUM.Core.Common.Attributes;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.ServiceCatalog;

public class ServiceCatalogService : InfrastructureBase, IServiceCatalogService
{
    private const string BaseRoutePrefix = "/api/base/";
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private static readonly JsonElement NullElement = JsonDocument.Parse("null").RootElement.Clone();

    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionProvider;
    private readonly IHttpClientsService _httpClientsService;

    public ServiceCatalogService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _apiDescriptionProvider = serviceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
    }

    public async Task<PaginatedApiResponse<ServiceCatalogEndpointDto>> GetEndpointsAsync(ServiceCatalogEndpointsQuery query, CancellationToken cancellationToken)
    {
        query = NormalizePaging(query);

        var descriptions = GetCatalogDescriptions();

        if (!string.IsNullOrWhiteSpace(query.HttpMethod))
        {
            var httpMethod = query.HttpMethod.Trim();

            descriptions = descriptions.Where(d => string.Equals(d.Description.HttpMethod, httpMethod, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Url))
        {
            var url = query.Url.Trim();

            descriptions = descriptions.Where(d => GetUrl(d.Description).Contains(url, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var q = query.Q.Trim();

            descriptions = descriptions.Where(d =>
                GetUrl(d.Description).Contains(q, StringComparison.OrdinalIgnoreCase) ||
                GetPropertyNames(d.Description, d.Action).Any(name => name.Contains(q, StringComparison.OrdinalIgnoreCase)));
        }

        var endpoints = descriptions.Select(d =>
        {
            var catalog = GetCatalogAttribute(d.Action);

            return new ServiceCatalogEndpointDto
            {
                HttpMethod = d.Description.HttpMethod!.ToUpperInvariant(),
                Url = GetUrl(d.Description),
                ControllerName = d.Action.ControllerName,
                ActionName = d.Action.ActionName,
                Name = catalog?.Name,
                Description = catalog?.Description
            };
        });

        var filtered = Sort(endpoints, query.SortBy, query.SortDirection).ToList();
        var page = filtered.Skip(query.PageStart!.Value).Take(query.PageSize!.Value).ToList();

        await FillStatusesAsync(query.MicroserviceId, page, cancellationToken);

        return new PaginatedApiResponse<ServiceCatalogEndpointDto>(query, page, filtered.Count);
    }

    private IEnumerable<string> GetPropertyNames(ApiDescription description, ControllerActionDescriptor action)
    {
        var manifest = BuildManifest(description, action);

        return Flatten(manifest.Parameters).Concat(Flatten(manifest.Response)).Select(p => p.Name);
    }

    private static IEnumerable<ServiceCatalogPropertyDto> Flatten(IEnumerable<ServiceCatalogPropertyDto> properties)
    {
        foreach (var property in properties)
        {
            yield return property;

            foreach (var child in Flatten(property.Children))
            {
                yield return child;
            }
        }
    }

    private static ServiceCatalogEndpointsQuery NormalizePaging(ServiceCatalogEndpointsQuery query)
    {
        var pageStart = query.PageStart is > 0 ? query.PageStart.Value : 0;
        var pageSize = query.PageSize is > 0 ? Math.Min(query.PageSize.Value, MaxPageSize) : DefaultPageSize;

        return query with { PageStart = pageStart, PageSize = pageSize };
    }

    private static IEnumerable<ServiceCatalogEndpointDto> Sort(IEnumerable<ServiceCatalogEndpointDto> endpoints, string? sortBy, SortDirection? direction)
    {
        Func<ServiceCatalogEndpointDto, string> key = (sortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "controllername" or "controller" => e => e.ControllerName,
            "actionname" or "action" => e => e.ActionName,
            "name" => e => e.Name ?? string.Empty,
            "httpmethod" => e => e.HttpMethod,
            _ => e => e.Url
        };

        var ordered = direction == SortDirection.Desc
            ? endpoints.OrderByDescending(key, StringComparer.OrdinalIgnoreCase)
            : endpoints.OrderBy(key, StringComparer.OrdinalIgnoreCase);

        return ordered
            .ThenBy(e => e.Url, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.HttpMethod, StringComparer.OrdinalIgnoreCase);
    }

    private async Task FillStatusesAsync(Guid? microserviceId, IReadOnlyList<ServiceCatalogEndpointDto> page, CancellationToken cancellationToken)
    {
        if (page.Count == 0)
        {
            return;
        }

        try
        {
            var parameters = new Dictionary<string, dynamic>
            {
                { "Domain", BiAppOptions.Domain },
                { "Endpoints", page.Select(e => new ServiceCatalogEndpointKeyDto { HttpMethod = e.HttpMethod, Url = e.Url }).ToList() }
            };

            if (microserviceId.HasValue)
            {
                parameters.Add("MicroserviceId", microserviceId.Value);
            }

            var response = await _httpClientsService.CallService<List<ServiceCatalogEndpointStatusDto>>(
                serviceId: Ids.Service.GetServiceCatalogStatuses.Id,
                parameters: parameters,
                cancellationToken: cancellationToken);

            if (!response.Success || response.Value is null)
            {
                Logger.LogWarning("Service catalog statuses could not be read: {Messages}", string.Join("; ", response.Messages.Select(m => m.Message)));

                return;
            }

            var statuses = response.Value
                .GroupBy(s => (s.HttpMethod.ToUpperInvariant(), s.Url.ToLowerInvariant()))
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var endpoint in page)
            {
                if (!statuses.TryGetValue((endpoint.HttpMethod.ToUpperInvariant(), endpoint.Url.ToLowerInvariant()), out var status))
                {
                    continue;
                }

                endpoint.Registered = status.Registered;
                endpoint.ServiceId = status.ServiceId;
                endpoint.ServiceHttpMethod = status.ServiceHttpMethod;
                endpoint.PreviewStatus = status.PreviewStatus;
                endpoint.PreviewId = status.PreviewId;
                endpoint.PreviewRequestedAt = status.PreviewRequestedAt;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            Logger.LogWarning(ex, "Service catalog statuses could not be read for {Domain}.", BiAppOptions.Domain);
        }
    }

    public async Task<ApiResponse<ServiceCatalogManifestDto>> GetManifestAsync(string? httpMethod, string? url, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<ServiceCatalogManifestDto>();

        if (string.IsNullOrWhiteSpace(httpMethod) || string.IsNullOrWhiteSpace(url))
        {
            await AddMessage(response, "service_catalog_endpoint_is_required", cancellationToken);

            return response;
        }

        var normalizedUrl = "/" + url.Trim().TrimStart('/');

        var match = GetCatalogDescriptions().FirstOrDefault(d =>
            string.Equals(d.Description.HttpMethod, httpMethod.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(GetUrl(d.Description), normalizedUrl, StringComparison.OrdinalIgnoreCase));

        if (match.Description is null)
        {
            await AddMessage(response, "service_catalog_endpoint_not_found", cancellationToken);

            return response;
        }

        response.Value = BuildManifest(match.Description, match.Action);

        return response;
    }

    public async Task<ApiResponse<ServiceCatalogPreviewStartedDto>> PreviewAsync(ServiceCatalogPreviewCommand command, CancellationToken cancellationToken)
    {
        return await SendToConfigurationAsync<ServiceCatalogPreviewStartedDto>(Ids.Service.PreviewInternalService.Id, command.HttpMethod, command.Url, null, cancellationToken);
    }

    public async Task<ApiResponse<JsonElement>> ApplyAsync(ServiceCatalogApplyCommand command, CancellationToken cancellationToken)
    {
        if (IsProduction())
        {
            var disabled = new ApiResponse<JsonElement>();

            await AddMessage(disabled, "service_catalog_disabled_in_production", cancellationToken);

            return EnsureValue(disabled);
        }

        var previewId = command.PreviewId ?? Guid.Empty;
        var preview = await _httpClientsService.CallService<ServiceCatalogPreviewInfoDto>(
            serviceId: Ids.Service.GetServicePreview.Id,
            parameters: new Dictionary<string, dynamic> { { "Id", previewId } },
            cancellationToken: cancellationToken);

        if (!preview.Success || preview.Value is null)
        {
            var failed = new ApiResponse<JsonElement>();

            failed.AddMessage(preview.Messages);

            return EnsureValue(failed);
        }

        var decisions = new Dictionary<string, dynamic> { { "PreviewId", previewId } };

        return EnsureValue(await SendToConfigurationAsync<JsonElement>(Ids.Service.ApplyInternalService.Id, preview.Value.HttpMethod, preview.Value.Url, decisions, cancellationToken));
    }

    private bool IsProduction() => string.Equals(BiAppOptions.Environment, BiAppEnvironments.Production, StringComparison.OrdinalIgnoreCase);

    private static ApiResponse<JsonElement> EnsureValue(ApiResponse<JsonElement> response)
    {
        if (response.Value.ValueKind == JsonValueKind.Undefined)
        {
            response.Value = NullElement;
        }

        return response;
    }

    private async Task<ApiResponse<TResponse>> SendToConfigurationAsync<TResponse>(
        Guid serviceId,
        string? httpMethod,
        string? url,
        Dictionary<string, dynamic>? decisions,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<TResponse>();

        if (IsProduction())
        {
            await AddMessage(response, "service_catalog_disabled_in_production", cancellationToken);

            return response;
        }

        var manifestResponse = await GetManifestAsync(httpMethod, url, cancellationToken);

        if (!manifestResponse.Success || manifestResponse.Value is null)
        {
            response.AddMessage(manifestResponse.Messages);

            return response;
        }

        var parameters = new Dictionary<string, dynamic>
        {
            { "Manifest", manifestResponse.Value }
        };

        if (decisions is not null)
        {
            parameters.Add("Decisions", decisions);
        }

        return await _httpClientsService.CallService<TResponse>(serviceId: serviceId, parameters: parameters, cancellationToken: cancellationToken);
    }

    private ServiceCatalogManifestDto BuildManifest(ApiDescription description, ControllerActionDescriptor action)
    {
        var catalog = GetCatalogAttribute(action);
        var (responseType, isPaginated) = ServiceCatalogTypeDescriber.UnwrapResponseType(action.MethodInfo.ReturnType);

        var manifest = new ServiceCatalogManifestDto
        {
            Domain = BiAppOptions.Domain,
            HttpMethod = description.HttpMethod!.ToUpperInvariant(),
            Url = GetUrl(description),
            ControllerName = action.ControllerName,
            ActionName = action.ActionName,
            Name = catalog?.Name,
            Description = catalog?.Description,
            ResponseIsPaginated = isPaginated,
            Parameters = BuildParameters(description)
        };

        if (responseType is not null)
        {
            var elementType = ServiceCatalogTypeDescriber.GetCollectionElementType(responseType);

            manifest.ResponseIsArray = isPaginated || elementType is not null;
            manifest.Response = ServiceCatalogTypeDescriber.DescribeMembers(elementType ?? responseType, ServiceCatalogPropertySources.Response);
        }

        return manifest;
    }

    private static List<ServiceCatalogPropertyDto> BuildParameters(ApiDescription description)
    {
        var parameters = new List<ServiceCatalogPropertyDto>();

        foreach (var parameter in description.ParameterDescriptions)
        {
            var source = MapSource(parameter.Source);

            if (source is null || parameter.Type is null)
            {
                continue;
            }

            if (ServiceCatalogTypeDescriber.IsIgnoredBaseProperty(parameter.ModelMetadata?.ContainerType?.GetProperty(parameter.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)))
            {
                continue;
            }

            if (source == ServiceCatalogPropertySources.Body)
            {
                parameters.AddRange(ServiceCatalogTypeDescriber.DescribeMembers(parameter.Type, source));

                continue;
            }

            parameters.Add(ServiceCatalogTypeDescriber.Describe(parameter.Name, parameter.Type, source, !parameter.IsRequired));
        }

        return parameters
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    private static string? MapSource(BindingSource? source)
    {
        if (source == BindingSource.Query || source == BindingSource.ModelBinding)
        {
            return ServiceCatalogPropertySources.Query;
        }

        if (source == BindingSource.Body)
        {
            return ServiceCatalogPropertySources.Body;
        }

        if (source == BindingSource.Path)
        {
            return ServiceCatalogPropertySources.Route;
        }

        if (source == BindingSource.Form || source == BindingSource.FormFile)
        {
            return ServiceCatalogPropertySources.Form;
        }

        return null;
    }

    private IEnumerable<(ApiDescription Description, ControllerActionDescriptor Action)> GetCatalogDescriptions()
    {
        return _apiDescriptionProvider.ApiDescriptionGroups.Items
            .SelectMany(g => g.Items)
            .Where(d => !string.IsNullOrWhiteSpace(d.HttpMethod) && d.ActionDescriptor is ControllerActionDescriptor)
            .Select(d => (Description: d, Action: (ControllerActionDescriptor)d.ActionDescriptor))
            .Where(d => !GetUrl(d.Description).StartsWith(BaseRoutePrefix, StringComparison.OrdinalIgnoreCase))
            .Where(d => GetCatalogAttribute(d.Action)?.Enabled != false);
    }

    private static ServiceCatalogAttribute? GetCatalogAttribute(ControllerActionDescriptor action)
    {
        return action.MethodInfo.GetCustomAttribute<ServiceCatalogAttribute>(true)
            ?? action.ControllerTypeInfo.GetCustomAttribute<ServiceCatalogAttribute>(true);
    }

    private static string GetUrl(ApiDescription description)
    {
        var path = description.RelativePath ?? string.Empty;
        var queryIndex = path.IndexOf('?');

        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }

        return "/" + path.TrimStart('/');
    }
}