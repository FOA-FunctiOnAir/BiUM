using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;
using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;
using BiUM.Test.Domain.Entities;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<PaginatedApiResponse<GetFwCurrenciesForParameterDto>> GetFwCurrenciesForParameter(
        string? q,
        int? pageStart,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var currencies = await _context.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == _currentUserService.LanguageId))
            .Where(c =>
                (
                    string.IsNullOrEmpty(q) ||
                    (string.IsNullOrEmpty(c.Name) || c.Name.Contains(q)) ||
                    (string.IsNullOrEmpty(c.Code) || c.Code.Contains(q))
                )
            )
            .ToPaginatedListAsync<Currency, GetFwCurrenciesForParameterDto>(_mapper, pageStart, pageSize, cancellationToken);

        return currencies;
    }

    public async Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> GetFwCurrenciesForNames(IReadOnlyList<Guid>? ids, CancellationToken cancellationToken)
    {
        var returnObject = new ApiResponse<IList<GetFwCurrenciesForNamesDto>>();

        var currencies = await _context.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == _currentUserService.LanguageId))
            .Where(m => ids != null && ids.Contains(m.Id))
            .ToIListAsync<Currency, GetFwCurrenciesForNamesDto>(_mapper, cancellationToken);

        returnObject.Value = currencies;

        return returnObject;
    }
}