using BiUM.Contract.Models.Api;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Common.Translation;
using BiUM.Specialized.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Services;

public sealed partial class TranslationService
{
    public async Task<ApiResponse> SaveDomainTranslationAsync(
        SaveDomainTranslationCommand command,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        if (command.ApplicationId == Guid.Empty)
        {
            await AddMessage(response, "application_required", cancellationToken);

            return response;
        }

        var domainTranslation = await _baseContext.DomainTranslations.FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken);

        if (domainTranslation is null)
        {
            domainTranslation = new DomainTranslation
            {
                ApplicationId = command.ApplicationId,
                Code = command.Code,
                Test = command.Test
            };

            _baseContext.DomainTranslations.Add(domainTranslation);

            var domainTranslationDetails = command.Translations?.Select(p => new DomainTranslationDetail
            {
                TranslationId = domainTranslation.Id,
                LanguageId = p.LanguageId,
                Text = p.Text
            });

            if (domainTranslationDetails is not null)
            {
                _baseContext.DomainTranslationDetails.AddRange(domainTranslationDetails);
            }
        }
        else
        {
            domainTranslation.ApplicationId = command.ApplicationId;
            domainTranslation.Code = command.Code;
            domainTranslation.Test = command.Test;

            var translations = command.Translations ?? [];

            foreach (var domainTranslationDetail in translations)
            {
                switch (domainTranslationDetail._rowStatus)
                {
                    case RowStatuses.New:
                        {
                            var newDomainTranslationDetail = new DomainTranslationDetail
                            {
                                TranslationId = domainTranslation.Id,
                                LanguageId = domainTranslationDetail.LanguageId,
                                Text = domainTranslationDetail.Text
                            };

                            _baseContext.DomainTranslationDetails.Add(newDomainTranslationDetail);

                            break;
                        }

                    case RowStatuses.Edited:
                        {
                            var existingDomainTranslationDetail = await _baseContext.DomainTranslationDetails.FirstOrDefaultAsync(f => f.Id == domainTranslationDetail.Id, cancellationToken);

                            if (existingDomainTranslationDetail is null)
                            {
                                break;
                            }

                            existingDomainTranslationDetail.LanguageId = domainTranslationDetail.LanguageId;
                            existingDomainTranslationDetail.Text = domainTranslationDetail.Text;

                            _baseContext.DomainTranslationDetails.Update(existingDomainTranslationDetail);

                            break;
                        }

                    case RowStatuses.Deleted:
                        {
                            var newDomainTranslationDetail = await _baseContext.DomainTranslationDetails.FirstOrDefaultAsync(f => f.Id == domainTranslationDetail.Id, cancellationToken);

                            if (newDomainTranslationDetail is null)
                            {
                                break;
                            }

                            _baseContext.DomainTranslationDetails.Remove(newDomainTranslationDetail);

                            break;
                        }
                }
            }

            _baseContext.DomainTranslations.Update(domainTranslation);
        }

        await _baseContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<ApiResponse> DeleteDomainTranslationAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = new ApiResponse();

        var domainTranslation = await _baseContext.DomainTranslations
            .Include(s => s.DomainTranslationDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (domainTranslation is null)
        {
            await AddMessage(response, "domain_translation_not_found", cancellationToken);

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
        Guid? microserviceId,
        string? code,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var domainTranslations = await _baseContext.DomainTranslations
            .Where(a =>
                (string.IsNullOrEmpty(q) || a.DomainTranslationDetails.Any(rt => rt.LanguageId == _correlationContext.LanguageId && rt.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase))) &&
                (string.IsNullOrEmpty(code) || (!string.IsNullOrEmpty(a.Code) && a.Code.Contains(code, StringComparison.CurrentCultureIgnoreCase))))
            .ToPaginatedListAsync<DomainTranslation, DomainTranslationsDto>(_mapper, pageStart, pageSize, cancellationToken);

        if (domainTranslations.Value is not null && microserviceId.HasValue)
        {
            foreach (var domainTranslation in domainTranslations.Value)
            {
                domainTranslation.MicroserviceId = microserviceId.Value;
            }
        }

        return domainTranslations;
    }

    private async Task<DomainTranslation?> GetTranslation(string code, CancellationToken cancellationToken)
    {
        var translation = await _baseContext.DomainTranslations
            .AsNoTracking()
            .Include(dt => dt.DomainTranslationDetails.Where(dtd => dtd.LanguageId == _correlationContext.LanguageId))
            .Where(x => x.Code.Equals(code) && x.ApplicationId == _correlationContext.ApplicationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (translation is null || translation.DomainTranslationDetails.Count == 0)
        {
            return null;
        }

        return translation;
    }
}