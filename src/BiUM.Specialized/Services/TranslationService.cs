using AutoMapper;
using BiUM.Contract;
using BiUM.Core.Authorization;
using BiUM.Core.Common.API;
using BiUM.Core.Common.Configs;
using BiUM.Core.Common.Enums;
using BiUM.Core.Models;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Common.Translation;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services;

public sealed class TranslationService : ITranslationService
{
    private readonly IDbContext _baseContext;

    private readonly IMapper _mapper;
    private readonly CorrelationContext _correlationContext;
    private readonly BiAppOptions _biAppOptions;

    public TranslationService(IServiceProvider serviceProvider)
    {
        _baseContext = serviceProvider.GetRequiredService<IDbContext>();

        var correlationContextProvider = serviceProvider.GetRequiredService<ICorrelationContextProvider>();

        _mapper = serviceProvider.GetRequiredService<IMapper>();
        _correlationContext = correlationContextProvider.Get() ?? CorrelationContext.Empty;
        _biAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }

    public async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, string.Empty, severity, cancellationToken);
    }

    public async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, exception.GetFullMessage(), MessageSeverity.Error, cancellationToken);
    }

    public async Task<IApiResponse> AddMessage(
        IApiResponse response,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(response, code, exception.GetFullMessage(), severity, cancellationToken);
    }

    public async Task<IApiResponse> AddMessage(
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

        var message = translation.DomainTranslationDetails!.First().Text;

        response.AddMessage(new ResponseMessage
        {
            Code = $"{_biAppOptions.Domain}.{code}",
            Message = message,
            Exception = exception,
            Severity = severity
        });

        return response;
    }

    public async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, string.Empty, MessageSeverity.Error, cancellationToken);
    }

    public async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, string.Empty, severity, cancellationToken);
    }

    public async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, exception.GetFullMessage(), MessageSeverity.Error, cancellationToken);
    }

    public async Task<GrpcResponseMeta> AddMessage(
        GrpcResponseMeta meta,
        string code,
        Exception exception,
        MessageSeverity severity,
        CancellationToken cancellationToken)
    {
        return await AddMessage(meta, code, exception.GetFullMessage(), severity, cancellationToken);
    }

    public async Task<GrpcResponseMeta> AddMessage(
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

        var message = translation.DomainTranslationDetails!.First().Text;

        meta.Messages.Add(new GrpcResponseMessage()
        {
            Code = $"{_biAppOptions.Domain}.{code}",
            Message = message,
            Exception = exception,
            Severity = (int)severity
        });

        return meta;
    }

    public async Task<ApiEmptyResponse> SaveDomainTranslationAsync(
        SaveDomainTranslationCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        if (command.ApplicationId == Guid.Empty)
        {
            response.AddMessage("Application is required", MessageSeverity.Error);

            return response;
        }

        var domainTranslation = await _baseContext.DomainTranslations.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (domainTranslation is null)
        {
            domainTranslation = new DomainTranslation()
            {
                ApplicationId = command.ApplicationId,
                Code = command.Code,
                Test = command.Test
            };

            domainTranslation.DomainTranslationDetails = command.Translations?.Select(p => new DomainTranslationDetail
            {
                TranslationId = domainTranslation.Id,
                LanguageId = p.LanguageId,
                Text = p.Text
            }).ToList();

            _baseContext.DomainTranslations.Add(domainTranslation);
        }
        else
        {
            domainTranslation.ApplicationId = command.ApplicationId;
            domainTranslation.Code = command.Code;
            domainTranslation.Test = command.Test;

            var translations = command.Translations ?? [];

            foreach (var domainTranslationDetail in translations)
            {
                if (domainTranslationDetail._rowStatus == RowStatuses.New)
                {
                    var newDomainTranslationDetail = new DomainTranslationDetail
                    {
                        TranslationId = domainTranslation.Id,
                        LanguageId = domainTranslationDetail.LanguageId,
                        Text = domainTranslationDetail.Text
                    };

                    _baseContext.DomainTranslationDetails.Add(newDomainTranslationDetail);
                }
                else if (domainTranslationDetail._rowStatus == RowStatuses.Edited)
                {
                    var newDomainTranslationDetail = await _baseContext.DomainTranslationDetails.FirstOrDefaultAsync(f => f.Id == domainTranslationDetail.Id, cancellationToken);

                    newDomainTranslationDetail!.LanguageId = domainTranslationDetail.LanguageId;
                    newDomainTranslationDetail.Text = domainTranslationDetail.Text;

                    _baseContext.DomainTranslationDetails.Update(newDomainTranslationDetail);
                }
                else if (domainTranslationDetail._rowStatus == RowStatuses.Deleted)
                {
                    var newDomainTranslationDetail = await _baseContext.DomainTranslationDetails.FirstOrDefaultAsync(f => f.Id == domainTranslationDetail.Id, cancellationToken);

                    _baseContext.DomainTranslationDetails.Remove(newDomainTranslationDetail!);
                }
            }

            _baseContext.DomainTranslations.Update(domainTranslation);
        }

        await _baseContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiEmptyResponse> DeleteDomainTranslationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiEmptyResponse();

        var domainTranslation = await _baseContext.DomainTranslations
            .Include(s => s.DomainTranslationDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainTranslation is null)
        {
            response.AddMessage("Domain Translation not found", MessageSeverity.Error);

            return response;
        }

        _baseContext.DomainTranslations.Remove(domainTranslation);

        await _baseContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiResponse<DomainTranslationDto>> GetDomainTranslationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<DomainTranslationDto>();

        var domainTranslation = await _baseContext.DomainTranslations
            .Include(m => m.DomainTranslationDetails)
            .FirstOrDefaultAsync<DomainTranslation, DomainTranslationDto>(x => x.Id == id, _mapper, cancellationToken);

        returnObject.Value = domainTranslation;

        return returnObject;
    }

    public async Task<PaginatedApiResponse<DomainTranslationsDto>> GetDomainTranslationsAsync(
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var domainTranslations = await _baseContext.DomainTranslations
            .Where(a =>
                (string.IsNullOrEmpty(q) || a.DomainTranslationDetails!.Any(rt => rt.LanguageId == _correlationContext.LanguageId && rt.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(a.Code) && a.Code.Contains(code, StringComparison.CurrentCultureIgnoreCase))))
            .ToPaginatedListAsync<DomainTranslation, DomainTranslationsDto>(_mapper, pageStart, pageSize, cancellationToken);

        return domainTranslations;
    }

    private async Task<DomainTranslation?> GetTranslation(string code, CancellationToken cancellationToken)
    {
        var translation = await _baseContext.DomainTranslations
            .AsNoTracking()
            .Include(dt => dt.DomainTranslationDetails!.Where(dtd => dtd.LanguageId == _correlationContext.LanguageId))
            .Where(x => x.Code.Equals(code) && x.ApplicationId == _correlationContext.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (translation is null || translation.DomainTranslationDetails is null || translation.DomainTranslationDetails.Count == 0)
        {
            return null;
        }

        return translation;
    }
}
