using BiUM.Contract.Enums;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Dtos;
using BiUM.Test2.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetCurrencies;

public class GetAccountsQueryHandler : IPaginatedQueryHandler<GetAccountsQuery, AccountsDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetAccountsQueryHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<AccountsDto>> Handle(GetAccountsQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetAccounts(query.Id, query.Name, query.Code, query.PageStart, query.PageSize, cancellationToken);

        if (!repositoryResponse.Success || repositoryResponse.Value == null)
        {
            repositoryResponse.AddMessage("No Account found.", MessageSeverity.Error);

            return repositoryResponse;
        }

        return repositoryResponse;
    }
}
