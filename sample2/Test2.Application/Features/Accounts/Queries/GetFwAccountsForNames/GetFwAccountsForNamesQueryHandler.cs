using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetFwCurrenciesForNames;

public class GetFwAccountsForNamesQueryHandler : IForValuesQueryHandler<GetFwAccountsForNamesQuery, GetFwAccountsForNamesDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetFwAccountsForNamesQueryHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<IList<GetFwAccountsForNamesDto>>> Handle(GetFwAccountsForNamesQuery query, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.GetFwAccountsForNames(query.Ids, cancellationToken);

        return response;
    }
}
