using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetCurrency;

public class GetCurrencyQueryHandler : IQueryHandler<GetCurrencyQuery, string>
{
    private readonly IAccountRepository _accountRepository;

    public GetCurrencyQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<ApiResponse<string>> Handle(GetCurrencyQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query.Id);

        var response = await _accountRepository.GetCurrency(query.Id.Value, cancellationToken);

        return response;
    }
}