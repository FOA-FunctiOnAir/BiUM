using BiUM.Contract.Models.Api;
using BiUM.Core.Caching;
using BiUM.Core.Constants;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Models;
using BiUM.Specialized.Common.Translation;
using BiUM.Specialized.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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

            var idsToFetch = translations
                .Where(t => t._rowStatus is RowStatuses.Edited or RowStatuses.Deleted)
                .Select(t => t.Id)
                .Distinct()
                .ToList();

            var prefetched = idsToFetch.Count > 0
                ? await _baseContext.DomainTranslationDetails
                    .Where(d => idsToFetch.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id, cancellationToken)
                : new Dictionary<Guid, DomainTranslationDetail>();

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
                            if (!prefetched.TryGetValue(domainTranslationDetail.Id, out var existingDomainTranslationDetail))
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
                            if (!prefetched.TryGetValue(domainTranslationDetail.Id, out var toDelete))
                            {
                                break;
                            }

                            _baseContext.DomainTranslationDetails.Remove(toDelete);

                            break;
                        }
                }
            }

            _baseContext.DomainTranslations.Update(domainTranslation);
        }

        await _baseContext.SaveChangesAsync(cancellationToken);

        await InvalidateTranslationCacheAsync(command.ApplicationId, command.Code);

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
            .ToPaginatedListAsync<DomainTranslation, DomainTranslationsDto>(PaginationQuery.ToPageBaseQuery(pageStart, pageSize), _mapper, cancellationToken);

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
        var result = await GetTranslationFromCacheOrDbAsync(code, _correlationContext.ApplicationId, cancellationToken);

        if (result is not null)
        {
            return result;
        }

        if (_correlationContext.ApplicationId == Ids.Application.BiDynamic.Id)
        {
            return null;
        }

        return await GetTranslationFromCacheOrDbAsync(code, Ids.Application.BiDynamic.Id, cancellationToken);
    }

    private async Task<DomainTranslation?> GetTranslationFromCacheOrDbAsync(string code, Guid applicationId, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Translation.Build(_biAppOptions.Domain, applicationId, _correlationContext.LanguageId, code);

        if (_inMemoryClient is not null)
        {
            try
            {
                var l1 = await _inMemoryClient.GetAsync<DomainTranslation>(cacheKey);

                if (l1.Value is not null)
                {
                    return l1.Value;
                }
            }
            catch { }
        }

        if (_redisClient is not null)
        {
            try
            {
                var l2 = await _redisClient.GetAsync<DomainTranslation>(cacheKey);

                if (l2.Value is not null)
                {
                    if (_inMemoryClient is not null)
                    {
                        try { await _inMemoryClient.AddAsync(cacheKey, l2.Value, _translationCacheL1Ttl); } catch { }
                    }

                    return l2.Value;
                }
            }
            catch { }
        }

        var translation = await _baseContext.DomainTranslations
            .AsNoTracking()
            .Include(dt => dt.DomainTranslationDetails.Where(dtd => dtd.LanguageId == _correlationContext.LanguageId))
            .Where(x => x.Code.Equals(code) && x.ApplicationId == applicationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (translation is null || translation.DomainTranslationDetails.Count == 0)
        {
            return null;
        }

        if (_redisClient is not null)
        {
            try { await _redisClient.AddAsync(cacheKey, translation, _translationCacheL2Ttl); } catch { }
        }

        if (_inMemoryClient is not null)
        {
            try { await _inMemoryClient.AddAsync(cacheKey, translation, _translationCacheL1Ttl); } catch { }
        }

        return translation;
    }

    private async Task InvalidateTranslationCacheAsync(Guid applicationId, string code)
    {
        var pattern = CacheKeys.Translation.Pattern(_biAppOptions.Domain, applicationId, code);

        if (_redisClient is not null)
        {
            try
            {
                var keys = await _redisClient.ScanKeysAsync(pattern);

                foreach (var key in keys)
                {
                    try { await _redisClient.RemoveAsync(key); } catch { }
                }
            }
            catch { }
        }

        if (_inMemoryClient is not null)
        {
            try
            {
                var keys = await _inMemoryClient.ScanKeysAsync(pattern);

                foreach (var key in keys)
                {
                    try { await _inMemoryClient.RemoveAsync(key); } catch { }
                }
            }
            catch { }
        }
    }
}