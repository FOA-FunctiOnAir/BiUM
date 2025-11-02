using BiUM.Test.Application.Repositories;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;

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