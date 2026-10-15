using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Constants;
using BiUM.Core.PlatformIntegration;
using BiUM.Specialized.Common.API;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test2.API.Controllers;

[BiUMRoute("test")]
public class TestSchedulerEventController : ApiControllerBase
{
    private readonly IPlatformActionExecutor _platformActionExecutor;
    private readonly ICorrelationContextAccessor _correlationContextAccessor;

    public TestSchedulerEventController(IPlatformActionExecutor platformActionExecutor, ICorrelationContextAccessor correlationContextAccessor)
    {
        _platformActionExecutor = platformActionExecutor;
        _correlationContextAccessor = correlationContextAccessor;
    }

    [HttpPost]
    public async Task<IActionResult> Trigger([FromQuery] string eventCode, [FromQuery] Guid applicationId, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var previous = _correlationContextAccessor.CorrelationContext;

        _correlationContextAccessor.CorrelationContext = new CorrelationContext
        {
            CorrelationId = Guid.NewGuid(),
            ApplicationId = applicationId,
            TenantId = tenantId,
            LanguageId = CorrelationContext.DefaultLanguageId,
            ConnectionId = $"scheduler:test-{eventCode}",
            ClientHost = "scheduler"
        };

        try
        {
            var response = await _platformActionExecutor.ExecuteAsync(new PlatformActionRequest
            {
                ActionType = Ids.Parameter.SchedulerTriggerType.Values.PublishEvent,
                EventCode = eventCode
            }, cancellationToken);

            return Ok(response);
        }
        finally
        {
            _correlationContextAccessor.CorrelationContext = previous;
        }
    }
}