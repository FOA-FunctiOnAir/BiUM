using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.Consts;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Dtos;
using BiUM.Specialized.Consts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BiUM.Specialized.Services.HttpClients;

public class HttpClientService : IHttpClientsService
{
    private readonly HttpClientsOptions _httpClientOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly JsonSerializerOptions _serializerSsettings = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public HttpClientService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<HttpClientsOptions> httpClientOptions)
    {
        _httpClientOptions = httpClientOptions.Value;
        this._httpContextAccessor = httpContextAccessor;
    }

    public async Task<IApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiEmptyResponse();

        try
        {
            using var _httpClient = CreateRequest();

            var serviceParameters = GetParameters(new([new("Id", serviceId.ToString())]));
            var targetServiceUrl = _httpClientOptions.GetFullUrl("/api/configuration/Service/GetService");
            var targetServiceUrlWithParameters = GetGetUrl(targetServiceUrl, serviceParameters, true);

            var httpResponseServiceApi = await _httpClient.GetAsync(targetServiceUrlWithParameters, cancellationToken);

            if (!httpResponseServiceApi.IsSuccessStatusCode)
            {
                response.AddMessage("HttpClientService.GetServiceDefinition", httpResponseServiceApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData = await httpResponseServiceApi.Content.ReadAsStringAsync(cancellationToken);

            var serviceData = JsonSerializer.Deserialize<ApiResponse<ServiceDto>>(resultData, _serializerSsettings);

            if (!serviceData!.Success)
            {
                response.AddMessage(serviceData.Messages);

                return response;
            }
            else if (serviceData?.Value == null)
            {
                response.AddMessage("Api Response value error", MessageSeverity.Error);

                return response;
            }

            var url = _httpClientOptions.GetFullUrl(serviceData.Value.Url);

            parameters = GetParameters(parameters);

            HttpResponseMessage? httpResponseTargetApi = null;

            if (serviceData.Value.HttpType == Ids.Parameter.HttpType.Values.Get)
            {
                var targetUrl = GetGetUrl(url, parameters, true);

                httpResponseTargetApi = await _httpClient.GetAsync(targetUrl, cancellationToken);
            }
            else if (serviceData.Value.HttpType == Ids.Parameter.HttpType.Values.Post)
            {
                var contentSerialized = JsonSerializer.Serialize(parameters, _serializerSsettings);
                var content = new StringContent(contentSerialized, Encoding.UTF8, "application/json");

                httpResponseTargetApi = await _httpClient.PostAsync(url, content, cancellationToken);
            }

            if (httpResponseTargetApi == null || !httpResponseTargetApi.IsSuccessStatusCode)
            {
                response.AddMessage("GetTargetServiceDefinition", httpResponseTargetApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData2 = await httpResponseTargetApi.Content.ReadAsStringAsync(cancellationToken);

            if (serviceData.Value.IsExternal == true)
            {
            }
            else
            {
                var innerResponse = JsonSerializer.Deserialize<ApiEmptyResponse>(resultData2, _serializerSsettings);

                if (innerResponse != null)
                {
                    response = innerResponse;
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            response.AddMessage($"HttpClientService.CallService-{ex.Message}", ex.StackTrace, MessageSeverity.Error);
        }

        return response;
    }

    public async Task<IApiResponse<TType>> CallService<TType>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiResponse<TType>();

        try
        {
            using var _httpClient = CreateRequest();

            var serviceParameters = GetParameters(new([new("Id", serviceId.ToString())]));
            var targetServiceUrl = _httpClientOptions.GetFullUrl("/api/configuration/Service/GetService");
            var targetServiceUrlWithParameters = GetGetUrl(targetServiceUrl, serviceParameters, true);

            var httpResponseServiceApi = await _httpClient.GetAsync(targetServiceUrlWithParameters, cancellationToken);

            if (!httpResponseServiceApi.IsSuccessStatusCode)
            {
                response.AddMessage("HttpClientService.GetServiceDefinition", httpResponseServiceApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData = await httpResponseServiceApi.Content.ReadAsStringAsync(cancellationToken);

            var serviceData = JsonSerializer.Deserialize<ApiResponse<ServiceDto>>(resultData, _serializerSsettings);

            if (!serviceData!.Success)
            {
                response.AddMessage(serviceData.Messages);

                return response;
            }
            else if (serviceData?.Value == null)
            {
                response.AddMessage("Api Response value error", MessageSeverity.Error);

                return response;
            }

            var url = _httpClientOptions.GetFullUrl(serviceData.Value.Url);

            parameters = GetParameters(parameters, q, pageStart, pageSize);

            HttpResponseMessage? httpResponseTargetApi = null;

            if (serviceData.Value.HttpType == Ids.Parameter.HttpType.Values.Get)
            {
                var targetUrl = GetGetUrl(url, parameters, true);

                httpResponseTargetApi = await _httpClient.GetAsync(targetUrl, cancellationToken);
            }
            else if (serviceData.Value.HttpType == Ids.Parameter.HttpType.Values.Post)
            {
                var contentSerialized = JsonSerializer.Serialize(parameters, _serializerSsettings);
                var content = new StringContent(contentSerialized, Encoding.UTF8, "application/json");

                httpResponseTargetApi = await _httpClient.PostAsync(url, content, cancellationToken);
            }

            if (httpResponseTargetApi == null || !httpResponseTargetApi.IsSuccessStatusCode)
            {
                response.AddMessage("GetTargetServiceDefinition", httpResponseTargetApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData2 = await httpResponseTargetApi.Content.ReadAsStringAsync(cancellationToken);

            if (serviceData.Value.IsExternal == true)
            {
                response.Value = JsonSerializer.Deserialize<TType>(resultData2, _serializerSsettings);
            }
            else
            {
                var innerResponse = JsonSerializer.Deserialize<ApiResponse<TType>>(resultData2, _serializerSsettings);

                if (innerResponse != null)
                {
                    response = innerResponse;
                }
                else
                {
                    response.Value = default;
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            response.AddMessage($"HttpClientService.CallService-{ex.Message}", ex.StackTrace, MessageSeverity.Error);
        }

        return response;
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
        var response = new ApiResponse<TType>();

        try
        {
            using var _httpClient = CreateRequest();

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters, q, pageStart, pageSize);

            var targetUrlWithParameters = GetGetUrl(targetUrl, parameters, true);

            var httpResponseTargetApi = await _httpClient.GetAsync(targetUrlWithParameters, cancellationToken);

            if (httpResponseTargetApi == null || !httpResponseTargetApi.IsSuccessStatusCode)
            {
                response.AddMessage("HttpClientService.GetTargetServiceDefinition", httpResponseTargetApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData2 = await httpResponseTargetApi.Content.ReadAsStringAsync(cancellationToken);

            if (external == true)
            {
                response.Value = JsonSerializer.Deserialize<TType>(resultData2, _serializerSsettings);
            }
            else
            {
                var innerResponse = JsonSerializer.Deserialize<ApiResponse<TType>>(resultData2, _serializerSsettings);

                if (innerResponse != null)
                {
                    response = innerResponse;
                }
                else
                {
                    response.Value = default;
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            response.AddMessage("HttpClientService.Get", ex.Message, MessageSeverity.Error);
        }

        return response;
    }

    public async Task<IApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiEmptyResponse();

        try
        {
            using var _httpClient = CreateRequest();

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters);

            var contentSerialized = JsonSerializer.Serialize(parameters, _serializerSsettings);
            var content = new StringContent(contentSerialized, Encoding.UTF8, "application/json");

            var httpResponseTargetApi = await _httpClient.PostAsync(targetUrl, content, cancellationToken);

            if (httpResponseTargetApi == null || !httpResponseTargetApi.IsSuccessStatusCode)
            {
                response.AddMessage("HttpClientService.GetTargetServiceDefinition", httpResponseTargetApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData2 = await httpResponseTargetApi.Content.ReadAsStringAsync(cancellationToken);

            if (external == false)
            {
                response = JsonSerializer.Deserialize<ApiEmptyResponse>(resultData2, _serializerSsettings);
            }

            return response;
        }
        catch (Exception ex)
        {
            response.AddMessage("HttpClientService.Post", ex.Message, MessageSeverity.Error);
        }

        return response;
    }

    public async Task<IApiResponse<TType>> Post<TType>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiResponse<TType>();

        try
        {
            using var _httpClient = CreateRequest();

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters);

            var contentSerialized = JsonSerializer.Serialize(parameters, _serializerSsettings);
            var content = new StringContent(contentSerialized, Encoding.UTF8, "application/json");

            var httpResponseTargetApi = await _httpClient.PostAsync(targetUrl, content, cancellationToken);

            if (httpResponseTargetApi == null || !httpResponseTargetApi.IsSuccessStatusCode)
            {
                response.AddMessage("HttpClientService.GetTargetServiceDefinition", httpResponseTargetApi?.ReasonPhrase ?? "ReasonPhrase", MessageSeverity.Error);

                return response;
            }

            string resultData2 = await httpResponseTargetApi.Content.ReadAsStringAsync(cancellationToken);

            if (external == true)
            {
                response.Value = JsonSerializer.Deserialize<TType>(resultData2, _serializerSsettings);
            }
            else
            {
                var innerResponse = JsonSerializer.Deserialize<ApiResponse<TType>>(resultData2, _serializerSsettings);

                if (innerResponse != null)
                {
                    response = innerResponse;
                }
                else
                {
                    response.Value = default;
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            response.AddMessage("HttpClientService.Post", ex.Message, MessageSeverity.Error);
        }

        return response;
    }

    private HttpClient CreateRequest()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var correlationContextHeaderValue = httpContext?.Request.Headers[HeaderKeys.CorrelationContext];

        if (correlationContextHeaderValue.HasValue && correlationContextHeaderValue.Value != StringValues.Empty)
        {
            _httpClient.DefaultRequestHeaders.Add(HeaderKeys.CorrelationContext, correlationContextHeaderValue.Value.ToString());
        }

        _httpClient.Timeout = new TimeSpan(0, 5, 0);

        return _httpClient;
    }

    private static string GetGetUrl(string url, Dictionary<string, dynamic>? parameters = null, bool isGet = false)
    {
        string parameterizedUrl = url;

        if (parameters != null && parameters.Count > 0)
        {
            if (!parameterizedUrl.Contains('?'))
            {
                parameterizedUrl += "?";
            }

            var getParameters = parameters.Select(parameter =>
            {
                if (isGet && parameter.Value is IEnumerable enumerable and not string)
                {
                    var queryParts = new List<string>();

                    foreach (var item in enumerable)
                    {
                        if (item is null)
                        {
                            continue;
                        }

                        queryParts.Add($"{parameter.Key}={Uri.EscapeDataString(item.ToString()!)}");
                    }

                    return string.Join("&", queryParts);
                }

                return $"{parameter.Key}={parameter.Value}";
            });

            parameterizedUrl += string.Join("&", getParameters);
        }

        return parameterizedUrl;
    }

    private static Dictionary<string, dynamic> GetParameters(Dictionary<string, dynamic>? parameters, string? q = null, int? pageStart = null, int? pageSize = null)
    {
        parameters ??= [];

        if (!string.IsNullOrEmpty(q)) parameters.Add("Q", q);
        if (pageStart.HasValue) parameters.Add("PageStart", pageStart.Value.ToString());
        if (pageSize.HasValue) parameters.Add("PageSize", pageSize.Value.ToString());

        return parameters;
    }
}