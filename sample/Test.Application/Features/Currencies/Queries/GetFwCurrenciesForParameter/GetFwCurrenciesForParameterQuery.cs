using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public record GetFwCurrenciesForParameterQuery : BasePaginatedForValuesQueryDto<GetFwCurrenciesForParameterDto>;