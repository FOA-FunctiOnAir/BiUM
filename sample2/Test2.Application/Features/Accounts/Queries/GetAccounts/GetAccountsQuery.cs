using BiUM.Specialized.Common.MediatR;
using BiUM.Test2.Application.Dtos;

namespace BiUM.Test2.Application.Features.Accounts.Queries.GetCurrencies;

public record GetAccountsQuery : BasePaginatedQueryDto<AccountsDto>
{
    public string? Name { get; set; }

    public string? Code { get; set; }
}
