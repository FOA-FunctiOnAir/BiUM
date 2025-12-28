using BiUM.Specialized.Common.MediatR;
using BiUM.Test.Application.Dtos;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetCurrency;

public record GetCurrencyQuery : BaseQueryDto<CurrencyDto>
{
}
