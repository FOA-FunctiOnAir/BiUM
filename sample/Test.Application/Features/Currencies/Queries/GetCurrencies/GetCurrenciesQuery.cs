using BiUM.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetCurrencies;

public record GetCurrenciesQuery : BasePaginatedQueryDto<CurrenciesDto>
{
    public string? Name { get; set; }

    public string? Code { get; set; }
}