using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Dtos;
using BiUM.Test.Application.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetCurrency;

public class GetCurrencyQueryHandler : IQueryHandler<GetCurrencyQuery, CurrencyDto>
{
    private readonly ICurrencyRepository _currencyRepository;

    public GetCurrencyQueryHandler(ICurrencyRepository currencyRepository)
    {
        _currencyRepository = currencyRepository;
    }

    public async Task<ApiResponse<CurrencyDto>> Handle(GetCurrencyQuery query, CancellationToken cancellationToken)
    {
        var response = await _currencyRepository.GetCurrency(query.Id!.Value, cancellationToken);

        return response;
    }
}
