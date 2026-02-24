using BiApp.Test.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetCurrency;

public record GetCurrencyQuery : BaseQueryDto<CurrencyDto>;