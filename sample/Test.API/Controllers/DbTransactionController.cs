using BiApp.Test.Application.Features.Currencies.Commands.UpdateCurrencyCode;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BiApp.Test.API.Controllers;

[BiUMRoute("test")]
public class DbTransactionController : ApiControllerBase
{
    [HttpPost]
    public async Task<ApiResponse> UpdateCurrencyCode([FromBody] UpdateCurrencyCodeCommand query)
    {
        return await Mediator.Send(query);
    }
}