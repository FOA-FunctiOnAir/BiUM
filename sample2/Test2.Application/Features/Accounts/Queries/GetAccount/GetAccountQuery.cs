using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Dtos;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetAccount;

public record GetAccountQuery : BaseQueryDto<AccountDto>
{
}
