using BiApp.Test.Application.Dtos;
using BiApp.Test.Domain.Entities;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.Repositories;

public partial class CurrencyRepository
{
    public async Task<ApiResponse<CurrencyDto>> GetCurrency(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<CurrencyDto>();

        if (id != Guid.Empty)
        {
            var currency = await _context.Currencies
                .Include(c => c.CurrencyTranslations)
                .FirstOrDefaultAsync<Currency, CurrencyDto>(x => x.Id == id, Mapper, cancellationToken);

            if (currency is null)
            {
                await AddMessage(response, "CurrencyNotFound", cancellationToken);

                return response;
            }

            response.Value = currency;
        }

        return response;
    }

    public async Task<ApiResponse<CurrencyDto>> GetCurrencyByCode(string code, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<CurrencyDto>();

        var currency = await _context.Currencies
            .Include(c => c.CurrencyTranslations)
            .FirstOrDefaultAsync<Currency, CurrencyDto>(c => c.Code.Equals(code), Mapper, cancellationToken);

        if (currency is null)
        {
            await AddMessage(response, "CurrencyNotFound", cancellationToken);

            return response;
        }

        response.Value = currency;

        return response;
    }

    public async Task<PaginatedApiResponse<CurrenciesDto>> GetCurrencies(Guid? id, string? name, string? code, int? pageStart, int? pageSize, CancellationToken cancellationToken)
    {
        var currencys = _context.Currencies
            .Include(c => c.CurrencyTranslations.Where(ct => ct.LanguageId == CorrelationContext.LanguageId))
            .Where(p =>
                (!id.HasValue || p.Id == id.Value) &&
                (string.IsNullOrWhiteSpace(name) || p.Name.ToLower().Contains(name.Trim().ToLower())) &&
                (string.IsNullOrEmpty(code) || p.Code == code)
            );

        var result = await currencys.ToPaginatedListAsync<Currency, CurrenciesDto>(Mapper, pageStart ?? 0, pageSize ?? 10, cancellationToken);

        return result;
    }
}
