using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Dtos;
using BiUM.Specialized.Consts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BiUM.Specialized.Services.HttpClients;

public class HttpClientService : IHttpClientsService
{
    private const int UrlMaxLength = 2048;

    private const string JsonContentType = "application/json";

    private const string CorrelationContextHeader = "X-Correlation-Context";

    private static readonly TimeSpan Timeout = new TimeSpan(0, 5, 0);
    private static readonly MediaTypeHeaderValue JsonMediaTypeHeaderValue = new(JsonContentType);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClientsOptions _httpClientOptions;

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IOptions<HttpClientsOptions> httpClientOptions)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _httpClientOptions = httpClientOptions.Value;
    }

    public async Task<IApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ApiEmptyResponse();

        try
        {
            var serviceResult = await GetServiceInfoAsync(serviceId, cancellationToken);

            if (!serviceResult.Success)
            {
                result.AddMessage(serviceResult.Messages);

                return result;
            }

            if (serviceResult.Value is null)
            {
                result.AddMessage("Api Response value error", MessageSeverity.Error);

                return result;
            }

            var url = _httpClientOptions.GetFullUrl(serviceResult.Value.Url);

            parameters = AddSearchAndPagination(parameters);

            HttpResponseMessage? response = null;

            var httpClient = GetHttpClient(serviceId);

            if (serviceResult.Value.HttpType == Ids.Parameter.HttpType.Values.Get)
            {
                var targetUrl = AppendParametersAsQueryString(url, parameters);

                var request = CreateRequestMessage(HttpMethod.Get, targetUrl);

                TryAddCorrelationContext(request);

                response = await httpClient.SendAsync(request, cancellationToken);
            }
            else if (serviceResult.Value.HttpType == Ids.Parameter.HttpType.Values.Post)
            {
                var request = CreateRequestMessage(HttpMethod.Post, url);

                request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

                TryAddCorrelationContext(request);

                response = await httpClient.SendAsync(request, cancellationToken);
            }

            if (response is not { IsSuccessStatusCode: true })
            {
                result.AddMessage("GetTargetServiceDefinition", response?.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            if (serviceResult.Value.IsExternal == true)
            {
                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var fetchedResult = await JsonSerializer.DeserializeAsync<ApiEmptyResponse>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (fetchedResult is not null)
            {
                result = fetchedResult;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.CallService-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);
        }

        return result;
    }

    public async Task<IApiResponse<TType>> CallService<TType>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ApiResponse<TType>();

        try
        {
            var serviceResult = await GetServiceInfoAsync(serviceId, cancellationToken);

            if (!serviceResult.Success)
            {
                result.AddMessage(serviceResult.Messages);

                return result;
            }

            if (serviceResult.Value is null)
            {
                result.AddMessage("Api Response value error", MessageSeverity.Error);

                return result;
            }

            var url = _httpClientOptions.GetFullUrl(serviceResult.Value.Url);

            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            HttpResponseMessage? response = null;

            var httpClient = GetHttpClient(serviceId);

            if (serviceResult.Value.HttpType == Ids.Parameter.HttpType.Values.Get)
            {
                var targetUrl = AppendParametersAsQueryString(url, parameters);

                var request = CreateRequestMessage(HttpMethod.Get, targetUrl);

                TryAddCorrelationContext(request);

                response = await httpClient.SendAsync(request, cancellationToken);
            }
            else if (serviceResult.Value.HttpType == Ids.Parameter.HttpType.Values.Post)
            {
                var request = CreateRequestMessage(HttpMethod.Post, url);

                request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

                TryAddCorrelationContext(request);

                response = await httpClient.SendAsync(request, cancellationToken);
            }

            if (response is not { IsSuccessStatusCode: true })
            {
                result.AddMessage("GetTargetServiceDefinition", response?.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (serviceResult.Value.IsExternal == true)
            {
                result.Value = await JsonSerializer.DeserializeAsync<TType>(responseStream, _jsonSerializerOptions, cancellationToken);

                return result;
            }

            var fetchedResult = await JsonSerializer.DeserializeAsync<ApiResponse<TType>>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (fetchedResult is not null)
            {
                result = fetchedResult;
            }
            else
            {
                result.Value = default;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.CallService-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    public async Task<IApiResponse<TType>> Get<TType>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var result = new ApiResponse<TType>();

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            var httpClient = GetHttpClient(url);

            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            var targetUrl = AppendParametersAsQueryString(url, parameters);

            var request = CreateRequestMessage(HttpMethod.Get, targetUrl);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                result.AddMessage("HttpClientService.Get", response.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (external == true)
            {
                result.Value = await JsonSerializer.DeserializeAsync<TType>(responseStream, _jsonSerializerOptions, cancellationToken);

                return result;
            }

            var fetchedResponse = await JsonSerializer.DeserializeAsync<ApiResponse<TType>>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (fetchedResponse is not null)
            {
                result = fetchedResponse;
            }
            else
            {
                result.Value = default;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.Get-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    public async Task<IApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var result = new ApiEmptyResponse();

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            var httpClient = GetHttpClient(url);

            parameters = AddSearchAndPagination(parameters);

            var request = CreateRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                result.AddMessage("HttpClientService.Post", response.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            if (external == true)
            {
                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var fetchedResponse = await JsonSerializer.DeserializeAsync<ApiEmptyResponse>(responseStream, _jsonSerializerOptions, cancellationToken);
            if (fetchedResponse is not null)
            {
                result = fetchedResponse;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.Post-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    public async Task<IApiResponse<TType>> Post<TType>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var result = new ApiResponse<TType>();

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            var httpClient = GetHttpClient(url);

            parameters = AddSearchAndPagination(parameters);

            var request = CreateRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                result.AddMessage("HttpClientService.Post", response.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (external == true)
            {
                result.Value = await JsonSerializer.DeserializeAsync<TType>(responseStream, _jsonSerializerOptions, cancellationToken);

                return result;
            }

            var fetchedResponse = await JsonSerializer.DeserializeAsync<ApiResponse<TType>>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (fetchedResponse is not null)
            {
                result = fetchedResponse;
            }
            else
            {
                result.Value = default;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.Post-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    private HttpClient GetHttpClient(Guid serviceId)
    {
        var httpClient = _httpClientFactory.CreateClient(serviceId.ToString());

        httpClient.Timeout = Timeout;

        return httpClient;
    }

    private HttpClient GetHttpClient(string url)
    {
        var uri = new Uri(url);

        var host = uri.Host;

        var port = uri.Port;

        var serviceKey = uri.AbsolutePath.Split('/').FirstOrDefault(p => p != "api");

        var key = string.IsNullOrEmpty(serviceKey)
            ? $"{host}:{port}"
            : $"{host}:{port}/{serviceKey}";

        var httpClient = _httpClientFactory.CreateClient(key);

        httpClient.Timeout = Timeout;

        return httpClient;
    }

    private async Task<ApiResponse<ServiceDto>> GetServiceInfoAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        const string getServiceUrl = "/api/configuration/Service/GetService";

        try
        {
            var serviceParameters = AddSearchAndPagination(new([new("Id", serviceId.ToString())]));

            var serviceUrl = AppendParametersAsQueryString(_httpClientOptions.GetFullUrl(getServiceUrl), serviceParameters);

            var serviceRequest = CreateRequestMessage(HttpMethod.Get, serviceUrl);

            var serviceHttpClient = GetHttpClient(serviceUrl);

            var serviceResponse = await serviceHttpClient.SendAsync(serviceRequest, cancellationToken);

            if (!serviceResponse.IsSuccessStatusCode)
            {
                var result = new ApiResponse<ServiceDto>();

                result.AddMessage("HttpClientService.GetServiceDefinition", serviceResponse.ReasonPhrase ?? nameof(serviceResponse.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            var serviceResult = await JsonSerializer.DeserializeAsync<ApiResponse<ServiceDto>>(await serviceResponse.Content.ReadAsStreamAsync(cancellationToken), options: _jsonSerializerOptions, cancellationToken: cancellationToken);

            if (serviceResult is null)
            {
                var result = new ApiResponse<ServiceDto>();

                result.AddMessage("HttpClientService.GetServiceDefinition", "Failed to deserialize service response", MessageSeverity.Error);

                return result;
            }

            return serviceResult;
        }
        catch (Exception ex)
        {
            var result = new ApiResponse<ServiceDto>();

            result.AddMessage($"HttpClientService.GetServiceDefinition-{ex.GetType().Name}", ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    private static string AppendParametersAsQueryString(string url, Dictionary<string, dynamic>? parameters = null)
    {
        var sb = new StringBuilder(UrlMaxLength);

        sb.Append(url);

        if (parameters is not { Count: > 0 })
        {
            return sb.ToString();
        }

        if (!url.Contains('?'))
        {
            sb.Append('?');
        }

        foreach (var parameter in parameters)
        {
            if (parameter.Value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    sb.Append(parameter.Key);
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(item.ToString()!));
                    sb.Append('&');
                }
            }
            else
            {
                sb.Append(parameter.Key);
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(parameter.Value));
                sb.Append('&');
            }
        }

        return sb.ToString().TrimEnd('&');
    }

    private static Dictionary<string, dynamic> AddSearchAndPagination(Dictionary<string, dynamic>? parameters, string? q = null, int? pageStart = null, int? pageSize = null)
    {
        parameters ??= [];

        if (!string.IsNullOrEmpty(q))
        {
            parameters.Add("Q", q);
        }

        if (pageStart.HasValue)
        {
            parameters.Add("PageStart", pageStart.Value.ToString());
        }

        if (pageSize.HasValue)
        {
            parameters.Add("PageSize", pageSize.Value.ToString());
        }

        return parameters;
    }

    private static HttpRequestMessage CreateRequestMessage(HttpMethod method, string url) =>
        new(method, url);

    private void TryAddCorrelationContext(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return;
        }

        var correlationContextHeader = httpContext.Request.Headers[CorrelationContextHeader].ToString();

        if (string.IsNullOrEmpty(correlationContextHeader))
        {
            return;
        }

        request.Headers.Add(CorrelationContextHeader, correlationContextHeader);
    }
}