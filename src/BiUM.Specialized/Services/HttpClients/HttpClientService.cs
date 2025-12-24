using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Dtos;
using BiUM.Specialized.Consts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services.HttpClients;

public class HttpClientService : IHttpClientsService
{
    private const int UrlMaxLength = 2048;

    private const string JsonContentType = "application/json";

    private const string CorrelationContextHeader = "X-Correlation-Context";

    private static readonly TimeSpan Timeout = new(0, 5, 0);
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
        return await CallServiceBase<object>(
            serviceId: serviceId,
            parameters: parameters,
            q: null,
            pageStart: null,
            pageSize: null,
            returnValue: false,
            cancellationToken: cancellationToken);
    }

    public async Task<IApiResponse<TType>> CallService<TType>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return (IApiResponse<TType>)await CallServiceBase<TType>(
            serviceId: serviceId,
            parameters: parameters,
            q: q,
            pageStart: pageStart,
            pageSize: pageSize,
            returnValue: true,
            cancellationToken: cancellationToken);
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
            result.AddMessage($"HttpClientService.GetException-{ex.GetType().Name}", ex.GetFullMessage(), MessageSeverity.Error);

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
            result.AddMessage($"HttpClientService.PostException-{ex.GetType().Name}", ex.GetFullMessage(), MessageSeverity.Error);

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
            result.AddMessage($"HttpClientService.PostException<Type>-{ex.GetType().Name}", ex.GetFullMessage(), MessageSeverity.Error);

            return result;
        }
    }


    private async Task<IApiResponse> CallServiceBase<TType>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        bool returnValue = true,
        CancellationToken cancellationToken = default)
    {
        var result = returnValue ? new ApiResponse<TType>() : new ApiEmptyResponse();

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

            var service = serviceResult.Value;
            var isExternal = service.Type == Ids.Parameter.ServiceType.Values.External;

            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            var response = isExternal
                ? await ExecuteExternalCallAsync(service, parameters, cancellationToken)
                : await ExecuteInternalCallAsync(service, parameters, cancellationToken);

            if (response is not { IsSuccessStatusCode: true })
            {
                result.AddMessage("GetTargetServiceDefinition", response?.ReasonPhrase ?? nameof(response.ReasonPhrase), MessageSeverity.Error);

                return result;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (isExternal)
            {
                if (returnValue)
                {
                    ((IApiResponse<TType>)result).Value = await JsonSerializer.DeserializeAsync<TType>(responseStream, _jsonSerializerOptions, cancellationToken);
                }

                return result;
            }

            var fetchedResult = returnValue
                ? await JsonSerializer.DeserializeAsync<ApiResponse<TType>>(responseStream, _jsonSerializerOptions, cancellationToken)
                : await JsonSerializer.DeserializeAsync<ApiEmptyResponse>(responseStream, _jsonSerializerOptions, cancellationToken); ;

            if (fetchedResult is not null)
            {
                result = fetchedResult;
            }
            else if (returnValue)
            {
                ((IApiResponse<TType>)result).Value = default;
            }

            return result;
        }
        catch (Exception ex)
        {
            result.AddMessage($"HttpClientService.CallServiceBaseException<Type>-{ex.GetType().Name}", ex.GetFullMessage(), MessageSeverity.Error);

            return result;
        }
    }

    private HttpClient GetHttpClient(ServiceDto service)
    {
        var httpClient = _httpClientFactory.CreateClient(service.Id.ToString());

        var timeout = service.TimeoutMs.HasValue && service.TimeoutMs.Value > 0
            ? TimeSpan.FromMilliseconds(service.TimeoutMs.Value)
            : Timeout;

        httpClient.Timeout = timeout;

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

            result.AddMessage($"HttpClientService.GetServiceDefinitionException-{ex.GetType().Name}", ex.GetFullMessage(), MessageSeverity.Error);

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
            if (parameter.Value is null)
            {
                continue;
            }

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
                sb.Append(Uri.EscapeDataString(Convert.ToString(parameter.Value)));
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

    private async Task<HttpResponseMessage> ExecuteInternalCallAsync(
        ServiceDto service,
        Dictionary<string, dynamic>? parameters,
        CancellationToken cancellationToken)
    {
        string? url;

        if (service.Type == Ids.Parameter.ServiceType.Values.Crud || service.Type == Ids.Parameter.ServiceType.Values.DynamicApi)
        {
            if (string.IsNullOrEmpty(service.MicroserviceRootPath))
            {
                throw new InvalidOperationException("Microservice root path is not defined for the internal service call.");
            }

            url = _httpClientOptions.GetFullUrl(service.MicroserviceRootPath, service.Url);
        }
        else
        {
            url = _httpClientOptions.GetFullUrl(service.Url);
        }

        var httpClient = GetHttpClient(service);

        var httpMethod = ResolveHttpMethod(service.HttpType);

        HttpRequestMessage? request;

        if (httpMethod == HttpMethod.Get)
        {
            var targetUrl = AppendParametersAsQueryString(url, parameters);

            request = CreateRequestMessage(HttpMethod.Get, targetUrl);
        }
        else if (httpMethod == HttpMethod.Post)
        {
            request = CreateRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
        }
        else if (httpMethod == HttpMethod.Put)
        {
            request = CreateRequestMessage(HttpMethod.Put, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
        }
        else if (httpMethod == HttpMethod.Delete)
        {
            var targetUrl = AppendParametersAsQueryString(url, parameters);

            request = CreateRequestMessage(HttpMethod.Delete, targetUrl);
        }
        else
        {
            throw new NotSupportedException($"HTTP method '{httpMethod.Method}' is not supported for internal service calls.");
        }

        TryAddCorrelationContext(request);

        var response = await httpClient.SendAsync(request, cancellationToken);

        return response ?? new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
    }

    private async Task<HttpResponseMessage> ExecuteExternalCallAsync(
        ServiceDto service,
        Dictionary<string, dynamic>? parameters,
        CancellationToken cancellationToken)
    {
        var url = service.Url;

        var httpMethod = ResolveHttpMethod(service.HttpType);

        var authInfo = service.Authentication;

        if (authInfo is not null && authInfo.AuthType == Ids.Parameter.ServiceAuthType.Values.ApiKeyQuery)
        {
            if (!string.IsNullOrEmpty(authInfo.ApiKey))
            {
                var queryKeyName = string.IsNullOrEmpty(authInfo.ApiKeyHeaderName) ? "apiKey" : authInfo.ApiKeyHeaderName;

                parameters ??= [];

                parameters[queryKeyName] = authInfo.ApiKey;
            }
        }

        HttpRequestMessage request;

        if (httpMethod == HttpMethod.Get || httpMethod == HttpMethod.Delete)
        {
            url = AppendParametersAsQueryString(url, parameters);

            request = new HttpRequestMessage(httpMethod, url);
        }
        else if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
        {
            request = new HttpRequestMessage(httpMethod, url);

            if (parameters is not null && parameters.Count > 0)
            {
                request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
            }
        }
        else
        {
            request = new HttpRequestMessage(httpMethod, url);
        }

        if (authInfo is not null)
        {
            await ApplyAuthenticationAsync(request, authInfo, url, parameters, cancellationToken);
        }

        var httpClient = GetHttpClient(url);

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static HttpMethod ResolveHttpMethod(Guid httpType)
    {
        if (httpType == Ids.Parameter.HttpType.Values.Get)
        {
            return HttpMethod.Get;
        }

        if (httpType == Ids.Parameter.HttpType.Values.Post)
        {
            return HttpMethod.Post;
        }

        if (httpType == Ids.Parameter.HttpType.Values.Put)
        {
            return HttpMethod.Put;
        }

        if (httpType == Ids.Parameter.HttpType.Values.Delete)
        {
            return HttpMethod.Delete;
        }

        if (httpType == Ids.Parameter.HttpType.Values.Patch)
        {
            return HttpMethod.Patch;
        }

        return HttpMethod.Get;
    }

    private async Task ApplyAuthenticationAsync(
        HttpRequestMessage request,
        ServiceAuthenticationDto authInfo,
        string url,
        Dictionary<string, dynamic>? parameters,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return;
        }

        var authType = authInfo.AuthType;

        if (authType == Ids.Parameter.ServiceAuthType.Values.NoAuth)
        {
            return;
        }

        if (authType == Ids.Parameter.ServiceAuthType.Values.Basic)
        {
            if (!string.IsNullOrEmpty(authInfo.Username) && !string.IsNullOrEmpty(authInfo.Password))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{authInfo.Username}:{authInfo.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.BearerStatic)
        {
            if (!string.IsNullOrEmpty(authInfo.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authInfo.ApiKey);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.ApiKeyHeader)
        {
            if (!string.IsNullOrEmpty(authInfo.ApiKey))
            {
                var headerName = string.IsNullOrEmpty(authInfo.ApiKeyHeaderName) ? "X-Api-Key" : authInfo.ApiKeyHeaderName;
                request.Headers.Add(headerName, authInfo.ApiKey);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.CustomHeader)
        {
            if (!string.IsNullOrEmpty(authInfo.CustomHeadersJson))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(authInfo.CustomHeadersJson, _jsonSerializerOptions);

                    if (headers is not null)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.Add(header.Key, header.Value);
                        }
                    }
                }
                catch
                {
                }
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.OAuth2ClientCredentials)
        {
            var token = await GetOAuth2ClientCredentialsTokenAsync(authInfo, cancellationToken);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.OAuth2Password)
        {
            var token = await GetOAuth2PasswordTokenAsync(authInfo, cancellationToken);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }

    private async Task<string?> GetOAuth2ClientCredentialsTokenAsync(
        ServiceAuthenticationDto authInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(authInfo.TokenUrl))
        {
            return null;
        }

        try
        {
            var httpClient = GetHttpClient(authInfo.TokenUrl);

            var requestContent = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials")
            };

            if (!string.IsNullOrEmpty(authInfo.ClientId))
            {
                requestContent.Add(new KeyValuePair<string, string>("client_id", authInfo.ClientId));
            }

            if (!string.IsNullOrEmpty(authInfo.ClientSecret))
            {
                requestContent.Add(new KeyValuePair<string, string>("client_secret", authInfo.ClientSecret));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, authInfo.TokenUrl)
            {
                Content = new FormUrlEncodedContent(requestContent)
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tokenResponse = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                return accessTokenElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> GetOAuth2PasswordTokenAsync(
        ServiceAuthenticationDto authInfo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(authInfo.TokenUrl))
        {
            return null;
        }

        try
        {
            var httpClient = GetHttpClient(authInfo.TokenUrl);

            var requestContent = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password")
            };

            if (!string.IsNullOrEmpty(authInfo.Username))
            {
                requestContent.Add(new KeyValuePair<string, string>("username", authInfo.Username));
            }

            if (!string.IsNullOrEmpty(authInfo.Password))
            {
                requestContent.Add(new KeyValuePair<string, string>("password", authInfo.Password));
            }

            if (!string.IsNullOrEmpty(authInfo.ClientId))
            {
                requestContent.Add(new KeyValuePair<string, string>("client_id", authInfo.ClientId));
            }

            if (!string.IsNullOrEmpty(authInfo.ClientSecret))
            {
                requestContent.Add(new KeyValuePair<string, string>("client_secret", authInfo.ClientSecret));
            }

            if (!string.IsNullOrEmpty(authInfo.Audience))
            {
                requestContent.Add(new KeyValuePair<string, string>("audience", authInfo.Audience));
            }

            if (!string.IsNullOrEmpty(authInfo.Scope))
            {
                requestContent.Add(new KeyValuePair<string, string>("scope", authInfo.Scope));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, authInfo.TokenUrl)
            {
                Content = new FormUrlEncodedContent(requestContent)
            };

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tokenResponse = await JsonSerializer.DeserializeAsync<JsonElement>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (tokenResponse.TryGetProperty("access_token", out var accessTokenElement))
            {
                return accessTokenElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}