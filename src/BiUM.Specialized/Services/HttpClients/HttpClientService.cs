using AutoMapper;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Dtos;
using BiUM.Specialized.Consts;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BiUM.Infrastructure.Services.HttpClients;

public class HttpClientService : IHttpClientsService
{
    private readonly HttpClientsOptions _httpClientOptions;
    private readonly IMapper _mapper;

    private readonly JsonSerializerOptions _serializerSsettings = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
    };

    public HttpClientService(IOptions<HttpClientsOptions> httpClientOptions, IMapper mapper)
    {
        _httpClientOptions = httpClientOptions.Value;
        _mapper = mapper;
    }

    public async Task<IApiResponse<TType>> CallService<TType>(
        Guid correlationId,
        Guid serviceId,
        Guid tenantId,
        Guid languageId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiResponse<TType>();

        try
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Add("CorrelationId", correlationId.ToString());
            _httpClient.DefaultRequestHeaders.Add("LanguageId", languageId.ToString());

            _httpClient.Timeout = new TimeSpan(0, 5, 0);

            var serviceParameters = GetParameters(new([new("Id", serviceId.ToString())]), tenantId, languageId);
            var targetServiceUrl = _httpClientOptions.GetFullUrl("/api/configuration/Service/GetService");
            var targetServiceUrlWithParameters = GetGetUrl(targetServiceUrl, serviceParameters);

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

            parameters = GetParameters(parameters, tenantId, languageId, q, pageStart, pageSize);

            HttpResponseMessage? httpResponseTargetApi = null;

            if (serviceData.Value.HttpType == Ids.Parameter.HttpType.Values.Get)
            {
                var targetUrl = GetGetUrl(url, parameters);

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
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
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
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Add("CorrelationId", correlationId.ToString());
            _httpClient.DefaultRequestHeaders.Add("LanguageId", languageId.ToString());

            _httpClient.Timeout = new TimeSpan(0, 5, 0);

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters, tenantId, languageId, q, pageStart, pageSize);

            var targetUrlWithParameters = GetGetUrl(targetUrl, parameters);

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
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiEmptyResponse();

        try
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Add("CorrelationId", correlationId.ToString());
            _httpClient.DefaultRequestHeaders.Add("LanguageId", languageId.ToString());

            _httpClient.Timeout = new TimeSpan(0, 5, 0);

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters, tenantId, languageId);

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
        Guid correlationId,
        Guid tenantId,
        Guid languageId,
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool? external = false,
        CancellationToken cancellationToken = default)
    {
        var response = new ApiResponse<TType>();

        try
        {
            var _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _httpClient.DefaultRequestHeaders.Add("CorrelationId", correlationId.ToString());
            _httpClient.DefaultRequestHeaders.Add("LanguageId", languageId.ToString());

            _httpClient.Timeout = new TimeSpan(0, 5, 0);

            var targetUrl = _httpClientOptions.GetFullUrl(url);

            parameters = GetParameters(parameters, tenantId, languageId);

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

    private static string GetGetUrl(string url, Dictionary<string, dynamic>? parameters = null)
    {
        string parameterizedUrl = url;

        if (parameters != null && parameters.Count > 0)
        {
            if (!parameterizedUrl.Contains('?'))
            {
                parameterizedUrl += "?";
            }

            var getParameters = parameters.Select(parameter => $"{parameter.Key}={parameter.Value}");

            parameterizedUrl += string.Join("&", getParameters);
        }

        return parameterizedUrl;
    }

    private static Dictionary<string, dynamic> GetParameters(Dictionary<string, dynamic>? parameters, Guid tenantId, Guid languageId, string? q = null, int? pageStart = null, int? pageSize = null)
    {
        parameters ??= [];

        parameters.Add("TenantId", tenantId.ToString());
        parameters.Add("LanguageId", languageId.ToString());

        if (!string.IsNullOrEmpty(q)) parameters.Add("Q", q);
        if (pageStart.HasValue) parameters.Add("PageStart", pageStart.Value.ToString());
        if (pageSize.HasValue) parameters.Add("PageSize", pageSize.Value.ToString());

        return parameters;
    }
}