using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Compensation;
using BiUM.Specialized.Services.Compensation;
using BiUM.Specialized.Services.Crud;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

public sealed class CompensatableApiActionFilter : IAsyncActionFilter
{
    private const string LocalOrchestrationKey = "BiUM.Compensation.LocalOrchestration";

    private readonly ICorrelationContextAccessor _correlationContextAccessor;
    private readonly ICrudService _crudService;
    private readonly ICompensationService _compensationService;
    private readonly ICompensationSessionFinalizedPublisher _compensationSessionFinalizedPublisher;

    public CompensatableApiActionFilter(
        ICorrelationContextAccessor correlationContextAccessor,
        ICrudService crudService,
        ICompensationService compensationService,
        ICompensationSessionFinalizedPublisher compensationSessionFinalizedPublisher)
    {
        _correlationContextAccessor = correlationContextAccessor;
        _crudService = crudService;
        _compensationService = compensationService;
        _compensationSessionFinalizedPublisher = compensationSessionFinalizedPublisher;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor cad)
        {
            await next();

            return;
        }

        var isMainCompensatableEndpoint =
            cad.MethodInfo.GetCustomAttribute<CompensatableApiAttribute>(inherit: true) is not null
            || cad.ControllerTypeInfo.GetCustomAttribute<CompensatableApiAttribute>(inherit: true) is not null;

        if (!isMainCompensatableEndpoint)
        {
            await next();

            return;
        }

        var ctx = _correlationContextAccessor.CorrelationContext;
        var incomingSession = ctx?.CompensationSessionId;
        var incomingWasEmpty = !incomingSession.HasValue || incomingSession.Value == Guid.Empty;

        var isCrudMutation =
            cad.ControllerTypeInfo.Name == nameof(CrudController) &&
            cad.ActionName is nameof(CrudController.SaveAsync) or nameof(CrudController.SavePartialAsync) or nameof(CrudController.DeleteAsync);

        var localOrchestration = false;

        if (isCrudMutation && context.RouteData.Values.TryGetValue("code", out var codeObj))
        {
            var code = codeObj?.ToString();

            if (!string.IsNullOrEmpty(code))
            {
                var compensatible = await _crudService.IsCrudMutationCompensatibleByCodeAsync(code, context.HttpContext.RequestAborted);

                if (compensatible && incomingWasEmpty && ctx is not null)
                {
                    var newSession = Guid.NewGuid();
                    _correlationContextAccessor.CorrelationContext = ctx.WithCompensationSessionId(newSession);
                    localOrchestration = true;
                }
            }
        }

        context.HttpContext.Items[LocalOrchestrationKey] = localOrchestration;

        if (!localOrchestration && incomingWasEmpty && _correlationContextAccessor.CorrelationContext is { } currentForSession)
        {
            var sid = currentForSession.CompensationSessionId;

            if (!sid.HasValue || sid.Value == Guid.Empty)
            {
                _correlationContextAccessor.CorrelationContext = currentForSession.WithCompensationSessionId(Guid.NewGuid());
            }
        }

        if (!localOrchestration)
        {
            await next();

            return;
        }

        try
        {
            await next();
        }
        catch
        {
            await TryFinalizeAsync(rollback: true, context.HttpContext.RequestAborted);

            throw;
        }

        await TryFinalizeAsync(rollback: IsFailureResult(context.Result), context.HttpContext.RequestAborted);
    }

    private static bool IsFailureResult(IActionResult? result)
    {
        if (result is ObjectResult ob)
        {
            if (ob.StatusCode is >= 400 and <= 599)
            {
                return true;
            }

            if (ob.Value is ApiResponse api && !api.Success)
            {
                return true;
            }
        }

        if (result is StatusCodeResult status && status.StatusCode >= 400)
        {
            return true;
        }

        return false;
    }

    private async Task TryFinalizeAsync(bool rollback, CancellationToken cancellationToken)
    {
        var sessionId = _correlationContextAccessor.CorrelationContext?.CompensationSessionId;

        if (sessionId is null || sessionId == Guid.Empty)
        {
            return;
        }

        if (rollback)
        {
            await _compensationService.RollbackSessionAsync(sessionId.Value, cancellationToken);
        }
        else
        {
            await _compensationService.CommitSessionAsync(sessionId.Value, cancellationToken);
        }

        await _compensationSessionFinalizedPublisher.PublishAsync(sessionId.Value, success: !rollback, cancellationToken);
    }
}