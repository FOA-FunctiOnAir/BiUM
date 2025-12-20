using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Dtos;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetCurrencies;

public record GetCurrenciesQuery : BasePaginatedQueryDto<CurrenciesDto>
{
    public string? Name { get; set; }

    public string? Code { get; set; }
}