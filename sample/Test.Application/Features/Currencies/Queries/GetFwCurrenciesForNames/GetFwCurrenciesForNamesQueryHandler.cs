using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;

public class GetFwCurrenciesForNamesQueryHandler : IForValuesQueryHandler<GetFwCurrenciesForNamesQuery, GetFwCurrenciesForNamesDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetFwCurrenciesForNamesQueryHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> Handle(GetFwCurrenciesForNamesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(query.Ids, nameof(query.Ids));

        var response = await _currencyRepository.GetFwCurrenciesForNames(query.Ids, cancellationToken);

        return response;
    }
}