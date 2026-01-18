using BiUM.Specialized.Common.API;
using BiUM.Test2.Application.Dtos;
using BiUM.Test2.Application.Features.Accounts.Commands.DeleteAccount;
using BiUM.Test2.Application.Features.Accounts.Commands.SaveAccount;
using BiUM.Test2.Application.Features.Accounts.Commands.UpdateBoltAccount;
using BiUM.Test2.Application.Features.Accounts.Queries.GetAccount;
using BiUM.Test2.Application.Features.Accounts.Queries.GetCurrencies;
using BiUM.Test2.Application.Features.Accounts.Queries.GetCurrency;
using BiUM.Test2.Application.Features.Accounts.Queries.GetFwCurrenciesForNames;
using BiUM.Test2.Application.Features.Accounts.Queries.GetFwCurrenciesForParameter;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiUM.Test2.API.Controllers;

[BiUMRoute("test2")]
public class TestAccountController : ApiControllerBase
{
    [HttpPost]
    public async Task<ApiEmptyResponse> UpdateBoltAccount([FromBody] UpdateBoltAccountCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost]
    public async Task<ApiEmptyResponse> SaveAccount([FromBody] SaveAccountCommand query)
    {
        return await Mediator.Send(query);
    }

    [HttpDelete]
    public async Task<ApiEmptyResponse> DeleteAccount([FromBody] DeleteAccountCommand query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<ApiResponse<string>> GetCurrency([FromQuery] GetCurrencyQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<ApiResponse<AccountDto>> GetAccount([FromQuery] GetAccountQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<AccountsDto>> GetAccounts([FromQuery] GetAccountsQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<GetFwAccountsForParameterDto>> GetFwAccountsForParameter([FromQuery] GetFwAccountsForParameterQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<ApiResponse<IList<GetFwAccountsForNamesDto>>> GetFwAccountsForNames([FromQuery] GetFwAccountsForNamesQuery query)
    {
        return await Mediator.Send(query);
    }
}
