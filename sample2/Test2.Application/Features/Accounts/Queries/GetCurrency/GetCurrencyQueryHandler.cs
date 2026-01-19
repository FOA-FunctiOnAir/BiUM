using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetCurrency;

public class GetCurrencyQueryHandler : IQueryHandler<GetCurrencyQuery, string>
{
    private readonly IAccountRepository _accountRepository;

    public GetCurrencyQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<ApiResponse<string>> Handle(GetCurrencyQuery query, CancellationToken cancellationToken)
    {
        var response = await _accountRepository.GetCurrency(query.Id!.Value, cancellationToken);

        return response;
    }
}
