using BiApp.Test2.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Core.Common.Utils;
using BiUM.Specialized.Common.MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForNames;

public class GetFwAccountsForNamesQueryHandler : IForValuesQueryHandler<GetFwAccountsForNamesQuery, GetFwAccountsForNamesDto>
{
    private readonly IAccountRepository _currencyRepository;

    public GetFwAccountsForNamesQueryHandler(IAccountRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<IList<GetFwAccountsForNamesDto>>> Handle(GetFwAccountsForNamesQuery query, CancellationToken cancellationToken)
    {
        if (ValidationHelper.CheckNull(query?.Ids))
        {
            return ApiResponse.EmptyArray<GetFwAccountsForNamesDto>();
        }

        var response = await _currencyRepository.GetFwAccountsForNames(query.Ids, cancellationToken);

        return response;
    }
}