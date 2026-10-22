using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Attributes;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
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

    private readonly IApiDescriptionGroupCollectionProvider _apiDescriptionProvider;
    private readonly IHttpClientsService _httpClientsService;

    public ServiceCatalogService(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _apiDescriptionProvider = serviceProvider.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
    }

    public ApiResponse<IList<ServiceCatalogEndpointDto>> GetEndpoints()
    {
        var endpoints = GetCatalogDescriptions()
            .Select(d =>
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
            })
            .OrderBy(e => e.Url, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.HttpMethod, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ApiResponse<IList<ServiceCatalogEndpointDto>>(endpoints);
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

    public async Task<ApiResponse<JsonElement>> PreviewAsync(ServiceCatalogPreviewCommand command, CancellationToken cancellationToken)
    {
        return await SendToConfigurationAsync(Ids.Service.PreviewInternalService.Id, command.HttpMethod, command.Url, null, cancellationToken);
    }

    public async Task<ApiResponse<JsonElement>> ApplyAsync(ServiceCatalogApplyCommand command, CancellationToken cancellationToken)
    {
        return await SendToConfigurationAsync(Ids.Service.ApplyInternalService.Id, command.HttpMethod, command.Url, command.Decisions, cancellationToken);
    }

    private async Task<ApiResponse<JsonElement>> SendToConfigurationAsync(
        Guid serviceId,
        string? httpMethod,
        string? url,
        JsonElement? decisions,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse<JsonElement>();

        if (string.Equals(BiAppOptions.Environment, BiAppEnvironments.Production, StringComparison.OrdinalIgnoreCase))
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

        if (decisions is { ValueKind: not JsonValueKind.Undefined and not JsonValueKind.Null })
        {
            parameters.Add("Decisions", decisions.Value);
        }

        return await _httpClientsService.CallService<JsonElement>(serviceId: serviceId, parameters: parameters, cancellationToken: cancellationToken);
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