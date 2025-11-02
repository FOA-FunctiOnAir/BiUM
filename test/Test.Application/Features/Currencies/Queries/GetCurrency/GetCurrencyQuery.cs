using BiUM.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetCurrency;

public record GetCurrencyQuery : BaseQueryDto<CurrencyDto>
{
}