using BiApp.Test2.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetAccount;

public record GetAccountQuery : BaseQueryDto<AccountDto>;