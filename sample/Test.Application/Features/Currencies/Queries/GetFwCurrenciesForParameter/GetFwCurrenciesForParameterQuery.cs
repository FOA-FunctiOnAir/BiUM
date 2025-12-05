using BiUM.Specialized.Common.MediatR;

namespace BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;

public record GetFwCurrenciesForParameterQuery : BasePaginatedForValuesQueryDto<GetFwCurrenciesForParameterDto>
{
}