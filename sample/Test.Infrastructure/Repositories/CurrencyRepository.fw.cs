using BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;
using BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;
using BiApp.Test.Domain.Entities;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<PaginatedApiResponse<GetFwCurrenciesForParameterDto>> GetFwCurrenciesForParameter(
        IReadOnlyList<Guid>? selectedIds,
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var query = _context.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == CorrelationContext.LanguageId))
            .Where(c =>
                string.IsNullOrEmpty(q) ||
                string.IsNullOrEmpty(c.Name) || c.Name.Contains(q) ||
                string.IsNullOrEmpty(c.Code) || c.Code.Contains(q)
            );

        var result = await query.ToPaginatedListAsync<Currency, GetFwCurrenciesForParameterDto>(Mapper, pageStart, pageSize, cancellationToken);

        await result.MergeSelectedIdsAsync(selectedIds, query, Mapper, cancellationToken);

        return result;
    }

    public async Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> GetFwCurrenciesForNames(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<IList<GetFwCurrenciesForNamesDto>>();

        var currencies = await _context.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == CorrelationContext.LanguageId))
            .Where(m => ids.Contains(m.Id))
            .ToIListAsync<Currency, GetFwCurrenciesForNamesDto>(Mapper, cancellationToken);

        returnObject.Value = currencies;

        return returnObject;
    }
}