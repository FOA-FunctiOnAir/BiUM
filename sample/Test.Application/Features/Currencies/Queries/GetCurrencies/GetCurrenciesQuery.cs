using BiApp.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetCurrencies;

public record GetCurrenciesQuery : BasePaginatedQueryDto<CurrenciesDto>
{
    public string? Name { get; set; }

    public string? Code { get; set; }
}
