using BiApp.Test2.Application.Dtos;
using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetAccount;

public class GetAccountQueryHandler : IQueryHandler<GetAccountQuery, AccountDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetAccountQueryHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<AccountDto>> Handle(GetAccountQuery query, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.GetAccount(query.Id!.Value, cancellationToken);

        return response;
    }
}
