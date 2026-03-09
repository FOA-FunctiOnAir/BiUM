using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

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

        return repositoryResponse;
    }
}