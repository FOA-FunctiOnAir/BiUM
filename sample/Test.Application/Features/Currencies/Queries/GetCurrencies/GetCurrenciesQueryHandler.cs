using BiApp.Test.Application.Dtos;
using BiApp.Test.Application.Repositories;
using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetCurrencies;

public class GetCurrenciesQueryHandler : IPaginatedQueryHandler<GetCurrenciesQuery, CurrenciesDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetCurrenciesQueryHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<CurrenciesDto>> Handle(GetCurrenciesQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetCurrencies(query.Id, query.Name, query.Code, query.PageStart, query.PageSize, cancellationToken);

        if (!repositoryResponse.Success || repositoryResponse.Value == null)
        {
            repositoryResponse.AddMessage("No Account found.", MessageSeverity.Error);

            return repositoryResponse;
        }

        return repositoryResponse;
    }
}
