using AutoMapper;
using BiUM.Contract;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Infrastructure.Common.Models;
using BiUM.Infrastructure.Services;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BiUM.Specialized.Services.Authorization;

public class TranslationService : ITranslationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDbContext _baseContext;

    public readonly ICurrentUserService _currentUserService;
    public readonly IMapper _mapper;

    public readonly BiAppOptions _biAppOptions;

    public TranslationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        _baseContext = _serviceProvider.GetRequiredService<IDbContext>();

        _currentUserService = _serviceProvider.GetRequiredService<ICurrentUserService>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();

        _biAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, exception.GetFullMessage(), MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, exception.GetFullMessage(), severity, cancellationToken);
    }

    public virtual async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        var translation = await GetTranslation(code, cancellationToken);

        if (translation is null)
        {
            response.AddMessage(new ResponseMessage
            {
                Code = $"{_biAppOptions.Domain}.{code}",
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

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, string.Empty, severity, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, exception.GetFullMessage(), MessageSeverity.Error, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, exception.GetFullMessage(), severity, cancellationToken);
    }

    public virtual async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        string exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        var translation = await GetTranslation(code, cancellationToken);

        if (translation is null)
        {
            meta.Messages.Add(new GrpcResponseMessage()
            {
                Code = $"{_biAppOptions.Domain}.{code}",
                Exception = exception,
                Severity = (int)severity
            });

            return meta;
        }

        var message = translation.DomainTranslationDetails.First().Text;

        meta.Messages.Add(new GrpcResponseMessage()
        {
            Code = $"{_biAppOptions.Domain}.{code}",
            Message = message,
            Exception = exception,
            Severity = (int)severity
        });

        return meta;
    }

    private async Task<DomainTranslation> GetTranslation(string code, CancellationToken cancellationToken)
    {
        var translation = await _baseContext.DomainTranslations
            .AsNoTracking()
            .Include(dt => dt.DomainTranslationDetails.Where(dtd => dtd.LanguageId == _currentUserService.LanguageId))
            .Where(x => x.Code.Equals(code) && x.ApplicationId == _currentUserService.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (translation is null || translation.DomainTranslationDetails is null || translation.DomainTranslationDetails.Count == 0)
        {
            return null;
        }

        return translation;
    }
}