using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.API;
using BiUM.Test.Application.Dtos;
using BiUM.Test.Application.Features.Currencies.Commands.DeleteCurrency;
using BiUM.Test.Application.Features.Currencies.Commands.SaveCurrency;
using BiUM.Test.Application.Features.Currencies.Commands.UpdateBoltCurrency;
using BiUM.Test.Application.Features.Currencies.Queries.GetCurrencies;
using BiUM.Test.Application.Features.Currencies.Queries.GetCurrency;
using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;
using BiUM.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForParameter;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiUM.Test.API.Controllers;

[BiUMRoute("test")]
public class TestCurrencyController : ApiControllerBase
{
    [HttpPost]
    public async Task<ApiResponse> UpdateBoltCurrency([FromBody] UpdateBoltCurrencyCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost]
    public async Task<ApiResponse> SaveCurrency([FromBody] SaveCurrencyCommand query)
    {
        return await Mediator.Send(query);
    }

    [HttpDelete]
    public async Task<ApiResponse> DeleteCurrency([FromBody] DeleteCurrencyCommand query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<ApiResponse<CurrencyDto>> GetCurrency([FromQuery] GetCurrencyQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<CurrenciesDto>> GetCurrencies([FromQuery] GetCurrenciesQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<PaginatedApiResponse<GetFwCurrenciesForParameterDto>> GetFwCurrenciesForParameter([FromQuery] GetFwCurrenciesForParameterQuery query)
    {
        return await Mediator.Send(query);
    }

    [HttpGet]
    public async Task<ApiResponse<IList<GetFwCurrenciesForNamesDto>>> GetFwCurrenciesForNames([FromQuery] GetFwCurrenciesForNamesQuery query)
    {
        return await Mediator.Send(query);
    }
}
