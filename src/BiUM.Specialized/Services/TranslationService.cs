using AutoMapper;
using BiUM.Contract.Enums;
using BiUM.Contract.Models;
using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Caching.InMemory;
using BiUM.Core.Caching.Redis;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services;

public sealed partial class TranslationService : ITranslationService
{
    private readonly IDbContext _baseContext;

    private readonly IMapper _mapper;
    private readonly CorrelationContext _correlationContext;
    private readonly BiAppOptions _biAppOptions;
    private readonly IRedisClient? _redisClient;
    private readonly IInMemoryClient? _inMemoryClient;

    private static readonly TimeSpan _translationCacheL2Ttl = TimeSpan.FromDays(1);
    private static readonly TimeSpan _translationCacheL1Ttl = TimeSpan.FromMinutes(5);

    public TranslationService(IServiceProvider serviceProvider)
    {
        _baseContext = serviceProvider.GetRequiredService<IDbContext>();

        var correlationContextProvider = serviceProvider.GetRequiredService<ICorrelationContextProvider>();

        _mapper = serviceProvider.GetRequiredService<IMapper>();
        _correlationContext = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        _biAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
        _redisClient = serviceProvider.GetService<IRedisClient>();
        _inMemoryClient = serviceProvider.GetService<IInMemoryClient>();
    }

    public Task AddMessage(
        ApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return FindAndSetMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public Task AddMessage(
        ApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return FindAndSetMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public Task AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return FindAndSetMessage(response, code, exception.ToString(), MessageSeverity.Error, cancellationToken);
    }

    public Task AddMessage(
        ApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return FindAndSetMessage(response, code, exception.ToString(), severity, cancellationToken);
    }

    public Task AddMessage(
        ApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return FindAndSetMessage(response, code, exception.ToString(), severity, cancellationToken);
    }

    public async Task<string?> GetValueAsync(string code, CancellationToken cancellationToken)
    {
        var translation = await GetTranslation(code, cancellationToken);

        if (translation is null || translation.DomainTranslationDetails.Count == 0)
        {
            return null;
        }

        return translation.DomainTranslationDetails.First().Text;
    }

    private async Task<ApiResponse> FindAndSetMessage(
        ApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        var translation = await GetTranslation(code, cancellationToken);

        if (translation is null || translation.DomainTranslationDetails.Count == 0)
        {
            response.AddMessage(new ResponseMessage
            {
                Code = "translation_not_found",
                Message = $"Translation not found for {code}",
                Exception = exception,
                Severity = severity
            });

            return response;
        }

        var message = translation.DomainTranslationDetails.First().Text;

        response.AddMessage(new ResponseMessage
        {
            Code = $"{_biAppOptions.Domain}.{code}",
            Message = message,
            Exception = exception,
            Severity = severity
        });

        return response;
    }
}