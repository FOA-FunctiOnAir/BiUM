using BiApp.Test2.Application.Dtos;
using BiUM.Specialized.Common.MediatR;

namespace BiApp.Test2.Application.Features.Accounts.Queries.GetAccounts;

public record GetAccountsQuery : BasePaginatedQueryDto<AccountsDto>
{
    public string? Name { get; set; }

    public string? Code { get; set; }
}