using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;

public class GetFwCurrenciesForNamesQueryHandler : IForValuesQueryHandler<GetFwCurrenciesForNamesQuery, GetFwCurrenciesForNamesDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetFwCurrenciesForNamesQueryHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> Handle(GetFwCurrenciesForNamesQuery query, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.GetFwCurrenciesForNames(query.Ids, cancellationToken);

        return response;
    }
}
