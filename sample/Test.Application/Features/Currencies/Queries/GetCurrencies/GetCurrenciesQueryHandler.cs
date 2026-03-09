using BiApp.Test.Application.Dtos;
using BiApp.Test.Application.Repositories;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common;
using BiUM.Specialized.Common.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetCurrencies;

public class GetCurrenciesQueryHandler : ApplicationBase, IPaginatedQueryHandler<GetCurrenciesQuery, CurrenciesDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetCurrenciesQueryHandler(IServiceProvider serviceProvider, ICurrencyRepository currencyRepository) : base(serviceProvider)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<PaginatedApiResponse<CurrenciesDto>> Handle(GetCurrenciesQuery query, CancellationToken cancellationToken)
    {
        var repositoryResponse = await _currencyRepository.GetCurrencies(
            query.Id,
            query.Name,
            query.Code,
            query,
            cancellationToken);

        if (!repositoryResponse.Success || repositoryResponse.Value == null)
        {
            await AddMessage(repositoryResponse, "Failed to retrieve Currencies.", cancellationToken);

            return repositoryResponse;
        }

        return repositoryResponse;
    }
}