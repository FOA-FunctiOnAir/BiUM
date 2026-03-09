using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;

public class GetFwAccountsForParameterQueryHandler : IPaginatedForValuesQueryHandler<GetFwAccountsForParameterQuery, GetFwAccountsForParameterDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetFwAccountsForParameterQueryHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<GetFwAccountsForParameterDto>> Handle(GetFwAccountsForParameterQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetFwAccountsForParameter(query.Q, query.PageStart, query.PageSize, cancellationToken);

        return repositoryResponse;
    }
}