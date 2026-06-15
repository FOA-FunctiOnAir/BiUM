using BiApp.Test2.Application.Features.CompensationTest.SaveCompensationItem;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;

/// <summary>
/// Bu endpoint dışarıdan (Sample/CompensationOrchestratorController) çağrılır.
/// x-correlation-context header'ında gelen CompensationSessionId'yi kullanarak
/// CompensationItem kaydeder. Kayıt sonrası COMPENSATION_ITEM.C_STATUS = "I" olur.
/// Session finalize edildiğinde (event ile):
///   - CommitSession → C_STATUS = "C"
///   - RollbackSession → kayıt silinir
/// </summary>
[BiUMRoute("test2")]
public class CompensationTestController : ApiControllerBase
{
    [HttpPost]
    public async Task<ApiResponse> SaveCompensationItem([FromBody] SaveCompensationItemCommand command)
    {
        return await Mediator.Send(command);
    }
}