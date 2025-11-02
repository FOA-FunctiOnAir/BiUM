using BiUM.Test.Application.Repositories;
using BiUM.Core.Common.Enums;
using BiUM.Specialized.Common.API;
using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public class GetFwCurrenciesForParameterQueryHandler : IPaginatedForValuesQueryHandler<GetFwCurrenciesForParameterQuery, GetFwCurrenciesForParameterDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetFwCurrenciesForParameterQueryHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<GetFwCurrenciesForParameterDto>> Handle(GetFwCurrenciesForParameterQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetFwCurrenciesForParameter(query.Q, query.PageStart, query.PageSize, cancellationToken);

        if (!repositoryResponse.Success || repositoryResponse.Value == null)
        {
            repositoryResponse.AddMessage("No Currency found.", MessageSeverity.Error);

            return repositoryResponse;
        }

        return repositoryResponse;
    }
}