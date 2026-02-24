using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;

public record GetFwAccountsForParameterQuery : BasePaginatedForValuesQueryDto<GetFwAccountsForParameterDto>;