using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.Constants;
using BiUM.Core.HttpClients;
using BiUM.Core.MessageBroker.Events;
using BiUM.Core.MessageBroker.RabbitMQ;
using BiUM.Core.Serialization;
using BiUM.Infrastructure.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Services.HttpClients;

public class HttpClientService : IHttpClientsService
{
    private const int UrlMaxLength = 2048;

    private const string JsonContentType = "application/json";

    private const string DeserializationFailedErrorCode = "deserialization_failed";
    private const string UnexpectedSuccessErrorCode = "unexpected_success_response";

    private static readonly TimeSpan Timeout = new(0, 5, 0);
    private static readonly MediaTypeHeaderValue JsonMediaTypeHeaderValue = new(JsonContentType);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ICorrelationContextSerializer _correlationContextSerializer;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IRabbitMQClient? _rabbitMQClient;
    private readonly BiAppOptions _appOptions;
    private readonly HttpClientsOptions _httpClientOptions;

    private readonly bool _isProductionLike;

    public HttpClientService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        ICorrelationContextAccessor correlationContextAccessor,
        ICorrelationContextSerializer correlationContextSerializer,
        JsonSerializerOptions jsonSerializerOptions,
        IRabbitMQClient? rabbitMQClient,
        IOptions<BiAppOptions> appOptionsAccessor,
        IOptions<HttpClientsOptions> httpClientOptionsAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _correlationContextAccessor = correlationContextAccessor;
        _correlationContextSerializer = correlationContextSerializer;
        _jsonSerializerOptions = jsonSerializerOptions;
        _rabbitMQClient = rabbitMQClient;
        _appOptions = appOptionsAccessor.Value;
        _httpClientOptions = httpClientOptionsAccessor.Value;

        _isProductionLike = environment.IsProductionLike(_appOptions);
    }

    public async Task<ApiResponse> CallService(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return await CallServiceBase<Void>(
            serviceId: serviceId,
            parameters: parameters,
            q: q,
            pageStart: pageStart,
            pageSize: pageSize,
            returnValue: false,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<TResponse>> CallService<TResponse>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        return (ApiResponse<TResponse>)await CallServiceBase<TResponse>(
            serviceId: serviceId,
            parameters: parameters,
            q: q,
            pageStart: pageStart,
            pageSize: pageSize,
            returnValue: true,
            cancellationToken: cancellationToken);
    }

    public async Task<ApiResponse<TResponse>> Get<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var originalUrl = url;
        string? finalUrl = null;
        var httpMethod = HttpMethod.Get;
        var isSuccessful = false;

        var startTimestamp = Stopwatch.GetTimestamp();
        TimeSpan? elapsed = null;

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            var httpClient = GetHttpClient(url);

            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            finalUrl = AppendParametersAsQueryString(url, parameters);

            var request = CreateRequestMessage(httpMethod, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            isSuccessful = response.IsSuccessStatusCode;

            var result =
                await TryDeserializeApiResponse<TResponse>(
                    response,
                    external: external,
                    isSuccessful: response.IsSuccessStatusCode,
                    cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            elapsed ??= Stopwatch.GetElapsedTime(startTimestamp);

            var result = new ApiResponse<TResponse>();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
        finally
        {
            await PublishServiceCalledEventAsync(
                serviceName: ExtractServiceName(finalUrl ?? originalUrl),
                endpoint: finalUrl ?? originalUrl,
                httpMethod: httpMethod,
                executionTimeMs: elapsed?.TotalMilliseconds is > 0 ? (long)elapsed.Value.TotalMilliseconds : 0,
                isSuccess: isSuccessful);
        }
    }

    public async Task<ApiResponse> Post(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default)
    {
        var originalUrl = url;
        string? finalUrl = null;
        var httpMethod = HttpMethod.Post;
        var isSuccessful = false;

        var startTimestamp = Stopwatch.GetTimestamp();
        TimeSpan? elapsed = null;

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            finalUrl = url;

            var httpClient = GetHttpClient(url);

            var request = CreateRequestMessage(httpMethod, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            isSuccessful = response.IsSuccessStatusCode;

            var result =
                await TryDeserializeApiResponse(
                    response,
                    external: external,
                    isSuccessful: response.IsSuccessStatusCode,
                    cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            elapsed ??= Stopwatch.GetElapsedTime(startTimestamp);

            var result = new ApiResponse();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
        finally
        {
            await PublishServiceCalledEventAsync(
                serviceName: ExtractServiceName(finalUrl ?? originalUrl),
                endpoint: finalUrl ?? originalUrl,
                httpMethod: httpMethod,
                executionTimeMs: elapsed?.TotalMilliseconds is > 0 ? (long)elapsed.Value.TotalMilliseconds : 0,
                isSuccess: isSuccessful);
        }
    }

    public async Task<ApiResponse<TResponse>> Post<TResponse>(
        string url,
        Dictionary<string, dynamic>? parameters = null,
        bool external = false,
        CancellationToken cancellationToken = default)
    {
        var originalUrl = url;
        string? finalUrl = null;
        var httpMethod = HttpMethod.Post;
        var isSuccessful = false;

        var startTimestamp = Stopwatch.GetTimestamp();
        TimeSpan? elapsed = null;

        try
        {
            url = _httpClientOptions.GetFullUrl(url);

            finalUrl = url;

            var httpClient = GetHttpClient(url);

            var request = CreateRequestMessage(httpMethod, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);

            TryAddCorrelationContext(request);

            var response = await httpClient.SendAsync(request, cancellationToken);

            elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            isSuccessful = response.IsSuccessStatusCode;

            var result =
                await TryDeserializeApiResponse<TResponse>(
                    response,
                    external: external,
                    isSuccessful: response.IsSuccessStatusCode,
                    cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            elapsed ??= Stopwatch.GetElapsedTime(startTimestamp);

            var result = new ApiResponse<TResponse>();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
        finally
        {
            await PublishServiceCalledEventAsync(
                serviceName: ExtractServiceName(finalUrl ?? originalUrl),
                endpoint: finalUrl ?? originalUrl,
                httpMethod: httpMethod,
                executionTimeMs: elapsed?.TotalMilliseconds is > 0 ? (long)elapsed.Value.TotalMilliseconds : 0,
                isSuccess: isSuccessful);
        }
    }


    private async Task<ApiResponse> CallServiceBase<TResponse>(
        Guid serviceId,
        Dictionary<string, dynamic>? parameters = null,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        bool returnValue = true,
        CancellationToken cancellationToken = default)
    {
        string? serviceUrl = null;
        string? finalUrl = null;
        HttpMethod? httpMethod = null;
        var isSuccessful = false;

        var startTimestamp = Stopwatch.GetTimestamp();
        TimeSpan? elapsed = null;

        try
        {
            var serviceResult = await GetServiceInfoAsync(serviceId, cancellationToken);

            if (!serviceResult.Success)
            {
                var result = returnValue ? new ApiResponse<TResponse>() : new ApiResponse();

                result.AddMessage(serviceResult);

                return result;
            }

            if (serviceResult.Value is null)
            {
                var result = returnValue ? new ApiResponse<TResponse>() : new ApiResponse();

                result.AddMessage("value_error", "Unable to get service info", MessageSeverity.Error);

                return result;
            }

            var service = serviceResult.Value;

            serviceUrl = service.Url;

            var isExternal = service.Type == Ids.Parameter.ServiceType.Values.External;

            var (rFinalUrl, rHttpMethod, response) =
                isExternal
                    ? await ExecuteExternalCallAsync(service, parameters, cancellationToken)
                    : await ExecuteInternalCallAsync(service, parameters, q, pageStart, pageSize, cancellationToken);

            elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            isSuccessful = response.IsSuccessStatusCode;

            finalUrl = rFinalUrl;
            httpMethod = rHttpMethod;

            {
                var result =
                    returnValue
                        ? await TryDeserializeApiResponse<TResponse>(
                            response,
                            external: isExternal,
                            isSuccessful: response.IsSuccessStatusCode,
                            cancellationToken: cancellationToken)
                        : await TryDeserializeApiResponse(
                            response,
                            external: isExternal,
                            isSuccessful: response.IsSuccessStatusCode,
                            cancellationToken: cancellationToken);

                return result;
            }
        }
        catch (Exception ex)
        {
            elapsed ??= Stopwatch.GetElapsedTime(startTimestamp);

            var result = returnValue ? new ApiResponse<TResponse>() : new ApiResponse();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
        finally
        {
            var url = finalUrl ?? serviceUrl ?? serviceId.ToString();

            await PublishServiceCalledEventAsync(
                serviceName: ExtractServiceName(url),
                endpoint: url,
                httpMethod: httpMethod ?? HttpMethod.Get,
                executionTimeMs: elapsed?.TotalMilliseconds is > 0 ? (long)elapsed.Value.TotalMilliseconds : 0,
                isSuccess: isSuccessful);
        }
    }

    private async Task<(string, HttpMethod, HttpResponseMessage)> ExecuteInternalCallAsync(
        ServiceDto service,
        Dictionary<string, dynamic>? parameters,
        string? q = null,
        int? pageStart = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        string? url;
        string? finalUrl;

        if (service.Type == Ids.Parameter.ServiceType.Values.Crud ||
            service.Type == Ids.Parameter.ServiceType.Values.DynamicApi)
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

        var httpMethod = ResolveHttpMethod(service.HttpType);

        HttpRequestMessage? request;

        if (httpMethod == HttpMethod.Get)
        {
            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            finalUrl = AppendParametersAsQueryString(url, parameters);

            request = CreateRequestMessage(HttpMethod.Get, finalUrl);
        }
        else if (httpMethod == HttpMethod.Post)
        {
            finalUrl = url;

            request = CreateRequestMessage(HttpMethod.Post, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
        }
        else if (httpMethod == HttpMethod.Put)
        {
            finalUrl = url;

            request = CreateRequestMessage(HttpMethod.Put, url);

            request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
        }
        else if (httpMethod == HttpMethod.Delete)
        {
            parameters = AddSearchAndPagination(parameters, q, pageStart, pageSize);

            finalUrl = AppendParametersAsQueryString(url, parameters);

            request = CreateRequestMessage(HttpMethod.Delete, finalUrl);
        }
        else
        {
            throw new NotSupportedException($"HTTP method '{httpMethod.Method}' is not supported for internal service calls.");
        }

        TryAddCorrelationContext(request);

        var httpClient = GetHttpClient(service);

        var response = await httpClient.SendAsync(request, cancellationToken);

        return (finalUrl, httpMethod, response);
    }

    private async Task<(string, HttpMethod, HttpResponseMessage)> ExecuteExternalCallAsync(
        ServiceDto service,
        Dictionary<string, dynamic>? parameters,
        CancellationToken cancellationToken)
    {
        var url = service.Url;

        string? finalUrl;

        var authentication = service.Authentication;

        if (authentication is not null && authentication.AuthType == Ids.Parameter.ServiceAuthType.Values.ApiKeyQuery)
        {
            if (!string.IsNullOrEmpty(authentication.ApiKey))
            {
                var queryKeyName =
                    string.IsNullOrEmpty(authentication.ApiKeyHeaderName)
                        ? "apiKey"
                        : authentication.ApiKeyHeaderName;

                parameters ??= [];

                parameters[queryKeyName] = authentication.ApiKey;
            }
        }

        var httpMethod = ResolveHttpMethod(service.HttpType);

        HttpRequestMessage request;

        if (httpMethod == HttpMethod.Post || httpMethod == HttpMethod.Put)
        {
            finalUrl = url;

            request = new HttpRequestMessage(httpMethod, finalUrl);

            if (parameters?.Count > 0)
            {
                request.Content = JsonContent.Create(parameters, JsonMediaTypeHeaderValue, _jsonSerializerOptions);
            }
        }
        else
        {
            finalUrl = AppendParametersAsQueryString(url, parameters);

            request = new HttpRequestMessage(httpMethod, finalUrl);
        }

        if (authentication is not null)
        {
            await ApplyAuthenticationAsync(request, authentication, cancellationToken);
        }

        var httpClient = GetHttpClient(finalUrl);

        var response = await httpClient.SendAsync(request, cancellationToken);

        return (finalUrl, httpMethod, response);
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

        string? finalUrl = null;
        var httpMethod = HttpMethod.Get;
        var isSuccessful = false;

        var startTimestamp = Stopwatch.GetTimestamp();
        TimeSpan? elapsed = null;

        try
        {
            var parameters = AddSearchAndPagination(new([new("Id", serviceId.ToString())]));

            finalUrl = AppendParametersAsQueryString(_httpClientOptions.GetFullUrl(getServiceUrl), parameters);

            var request = CreateRequestMessage(httpMethod, finalUrl);

            var httpClient = GetHttpClient(finalUrl);

            var response = await httpClient.SendAsync(request, cancellationToken);

            elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            isSuccessful = response.IsSuccessStatusCode;

            var result =
                await TryDeserializeApiResponse<ServiceDto>(
                    response,
                    external: false,
                    isSuccessful: response.IsSuccessStatusCode,
                    cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            elapsed ??= Stopwatch.GetElapsedTime(startTimestamp);

            var result = new ApiResponse<ServiceDto>();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
        finally
        {
            await PublishServiceCalledEventAsync(
                serviceName: ExtractServiceName(finalUrl ?? getServiceUrl),
                endpoint: finalUrl ?? getServiceUrl,
                httpMethod: httpMethod,
                executionTimeMs: elapsed?.TotalMilliseconds is > 0 ? (long)elapsed.Value.TotalMilliseconds : 0,
                isSuccess: isSuccessful);
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

        if (httpContext is not null)
        {
            var correlationContextHeader = httpContext.Request.Headers[HeaderKeys.CorrelationContext].ToString();

            if (!string.IsNullOrEmpty(correlationContextHeader))
            {
                request.Headers.Add(HeaderKeys.CorrelationContext, correlationContextHeader);

                return;
            }
        }

        var correlationContext = _correlationContextAccessor.CorrelationContext;

        if (correlationContext is not null)
        {
            var bytes = _correlationContextSerializer.Serialize(correlationContext);

            var base64 = Convert.ToBase64String(bytes);

            request.Headers.Add(HeaderKeys.CorrelationContext, base64);
        }
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
        ServiceAuthenticationDto authentication,
        CancellationToken cancellationToken)
    {
        var authType = authentication.AuthType;

        if (authType == Ids.Parameter.ServiceAuthType.Values.NoAuth)
        {
            return;
        }

        if (authType == Ids.Parameter.ServiceAuthType.Values.Basic)
        {
            if (!string.IsNullOrEmpty(authentication.Username) &&
                !string.IsNullOrEmpty(authentication.Password))
            {
                var credentials =
                    Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{authentication.Username}:{authentication.Password}"));

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.BearerStatic)
        {
            if (!string.IsNullOrEmpty(authentication.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication.ApiKey);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.ApiKeyHeader)
        {
            if (!string.IsNullOrEmpty(authentication.ApiKey))
            {
                var headerName = string.IsNullOrEmpty(authentication.ApiKeyHeaderName) ? "X-Api-Key" : authentication.ApiKeyHeaderName;

                request.Headers.Add(headerName, authentication.ApiKey);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.CustomHeader)
        {
            if (!string.IsNullOrEmpty(authentication.CustomHeadersJson))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(authentication.CustomHeadersJson, _jsonSerializerOptions);

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
                    // ignored
                }
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.OAuth2ClientCredentials)
        {
            var token = await GetOAuth2ClientCredentialsTokenAsync(authentication, cancellationToken);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else if (authType == Ids.Parameter.ServiceAuthType.Values.OAuth2Password)
        {
            var token = await GetOAuth2PasswordTokenAsync(authentication, cancellationToken);

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

    private async Task PublishServiceCalledEventAsync(
        string serviceName,
        string endpoint,
        HttpMethod httpMethod,
        long executionTimeMs,
        bool isSuccess,
        Guid? microserviceId = null,
        Guid? serviceId = null)
    {
        if (_rabbitMQClient is null)
        {
            return;
        }

        try
        {
            var serviceCalledEvent = new ServiceCalledEvent
            {
                MicroserviceId = microserviceId,
                ServiceId = serviceId,
                ServiceName = serviceName,
                Endpoint = endpoint,
                HttpMethod = httpMethod.Method,
                ExecutionTimeMs = executionTimeMs,
                Success = isSuccess
            };

            await _rabbitMQClient.PublishAsync(serviceCalledEvent);
        }
        catch
        {
            // ignored
        }
    }

    private string ExtractServiceName(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return _appOptions.Domain.ToLowerInvariant();
        }

        try
        {
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                return _appOptions.Domain.ToLowerInvariant();
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var serviceName = segments.Length > 1 && segments[0] == "api" ? segments[1] : uri.Host;

            return serviceName.ToLowerInvariant();
        }
        catch
        {
            return _appOptions.Domain.ToLowerInvariant();
        }
    }

    private async ValueTask<ApiResponse> TryDeserializeApiResponse(HttpResponseMessage response, bool isSuccessful = false, bool external = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (external)
            {
                var externalResult = new ApiResponse();

                if (!isSuccessful)
                {
                    externalResult.AddMessage(UnexpectedSuccessErrorCode, "The response was expected to be an error, but indicates success.", MessageSeverity.Warning);
                }

                return externalResult;
            }

            var result = await JsonSerializer.DeserializeAsync<ApiResponse>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (result is not null)
            {
                if (isSuccessful)
                {
                    return result;
                }

                if (result.Success)
                {
                    result.AddMessage(UnexpectedSuccessErrorCode, "The response was expected to be an error, but indicates success.", MessageSeverity.Warning);

                    return result;
                }
            }

            result = new ApiResponse();

            result.AddMessage(DeserializationFailedErrorCode, "Unable to deserialize the response", MessageSeverity.Warning);

            return result;
        }
        catch (Exception ex)
        {
            var result = new ApiResponse();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    private async ValueTask<ApiResponse<TResponse>> TryDeserializeApiResponse<TResponse>(HttpResponseMessage response, bool isSuccessful = false, bool external = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            if (external)
            {
                var externalValue = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _jsonSerializerOptions, cancellationToken);

                var externalResult = new ApiResponse<TResponse>
                {
                    Value = externalValue
                };

                if (!isSuccessful)
                {
                    externalResult.AddMessage(UnexpectedSuccessErrorCode, "The response was expected to be an error, but indicates success.", MessageSeverity.Warning);
                }

                return externalResult;
            }

            var result = await JsonSerializer.DeserializeAsync<ApiResponse<TResponse>>(responseStream, _jsonSerializerOptions, cancellationToken);

            if (result is not null)
            {
                if (isSuccessful)
                {
                    return result;
                }

                if (result.Success)
                {
                    result.AddMessage(UnexpectedSuccessErrorCode, "The response was expected to be an error, but indicates success.", MessageSeverity.Warning);

                    return result;
                }
            }

            result = new ApiResponse<TResponse>();

            result.AddMessage(DeserializationFailedErrorCode, "Unable to deserialize the response", MessageSeverity.Warning);

            return result;
        }
        catch (Exception ex)
        {
            var result = new ApiResponse<TResponse>();

            result.AddMessage(ex.ToErrorCode(), _isProductionLike ? ex.Message : ex.ToString(), MessageSeverity.Error);

            return result;
        }
    }

    private struct Void;
}