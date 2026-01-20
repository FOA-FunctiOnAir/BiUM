using BiApp.Test2.Application.Dtos;
using BiApp.Test2.Contract.Models.Rpc;
using BiApp.Test2.Domain.Entities;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Repositories;

public partial class AccountRepository
{
    public async Task<ApiResponse<string>> GetCurrency(Guid id, CancellationToken cancellationToken)
    {
        var response = new ApiResponse<string>();

        var getCurrencyNamesRequest = new GetCurrencyRequest();
        getCurrencyNamesRequest.CurrencyId = id.ToString();

        var currencyNamesResponse = await _testRpcService.WithCancellationToken(cancellationToken).GetCurrency(getCurrencyNamesRequest);

        if (!currencyNamesResponse.Success)
        {
            response.AddMessage(currencyNamesResponse.Messages);

            return response;
        }

        response.Value = currencyNamesResponse.Value?.CurrencyName;

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
