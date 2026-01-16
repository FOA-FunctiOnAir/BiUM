using BiUM.Specialized.Common.API;
using BiUM.Specialized.Database;
using BiUM.Test.Contract.Models;
using BiUM.Test2.Application.Dtos;
using BiUM.Test2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<ApiResponse<string>> GetCurrency(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<string>();

        var getCurrencyNamesRequest = new GetCurrencyRequest();
        getCurrencyNamesRequest.CurrencyId = id.ToString();

        var currencyNamesResponse = await _testRpcService.GetCurrency(getCurrencyNamesRequest);

        if (!currencyNamesResponse.Meta.Success)
        {
            response.AddMessage(currencyNamesResponse.Meta);

            return response;
        }

        response.Value = currencyNamesResponse.Currency.CurrencyName;

        return response;
    }

    public async Task<ApiResponse<AccountDto>> GetAccount(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<AccountDto>();

        if (id != Guid.Empty)
        {
            var account = await _context.Accounts
                .Include(c => c.AccountTranslations)
                .FirstOrDefaultAsync<Account, AccountDto>(x => x.Id == id, _mapper, cancellationToken);

            if (account is null)
            {
                await AddMessage(response, "AccountNotFound", cancellationToken);

                return response;
            }

            response.Value = account;
        }

        return response;
    }

    public async Task<ApiResponse<AccountDto>> GetAccountByCode(string code, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<AccountDto>();

        var account = await _context.Accounts
            .Include(c => c.AccountTranslations)
            .FirstOrDefaultAsync<Account, AccountDto>(c => c.Code.Equals(code), _mapper, cancellationToken);

        if (account is null)
        {
            await AddMessage(response, "AccountNotFound", cancellationToken);

            return response;
        }

        response.Value = account;

        return response;
    }

    public async Task<PaginatedApiResponse<AccountsDto>> GetAccounts(Guid? id, string? name, string? code, int? pageStart, int? pageSize, CancellationToken cancellationToken)
    {
        var currencys = _context.Accounts
            .Include(c => c.AccountTranslations.Where(ct => ct.LanguageId == _correlationContext.LanguageId))
            .Where(p =>
                (!id.HasValue || p.Id == id.Value) &&
                (string.IsNullOrWhiteSpace(name) || p.Name.ToLower().Contains(name.Trim().ToLower())) &&
                (string.IsNullOrEmpty(code) || p.Code == code)
            );

        var result = await currencys.ToPaginatedListAsync<Account, AccountsDto>(_mapper, pageStart ?? 0, pageSize ?? 10, cancellationToken);

        return result;
    }
}
