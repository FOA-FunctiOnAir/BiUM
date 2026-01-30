using BiApp.Test2.Application.Dtos;
using BiApp.Test2.Application.Features.Accounts.Commands.DeleteAccount;
using BiApp.Test2.Application.Features.Accounts.Commands.SaveAccount;
using BiApp.Test2.Application.Features.Accounts.Commands.UpdateBoltAccount;
using BiApp.Test2.Application.Features.Accounts.Queries.GetAccount;
using BiApp.Test2.Application.Features.Accounts.Queries.GetAccounts;
using BiApp.Test2.Application.Features.Accounts.Queries.GetCurrency;
using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForNames;
using BiApp.Test2.Application.Features.Accounts.Queries.GetFwAccountsForParameter;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;

[BiUMRoute("test2")]
public class TestAccountController : ApiControllerBase
{
    [HttpPost]
    public async Task<ApiResponse> UpdateBoltAccount([FromBody] UpdateBoltAccountCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost]
    public async Task<ApiResponse> SaveAccount([FromBody] SaveAccountCommand query)
    {
        return await Mediator.Send(query);
    }

    [HttpDelete]
    public async Task<ApiResponse> DeleteAccount([FromBody] DeleteAccountCommand query)
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