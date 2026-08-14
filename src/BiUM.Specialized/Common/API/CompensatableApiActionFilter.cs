using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Compensation;
using BiUM.Specialized.Services.Compensation;
using BiUM.Specialized.Services.Crud;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

public sealed class CompensatableApiActionFilter : IAsyncActionFilter
{
    // M-2: cache attribute presence per MemberInfo — reflection runs at most once per endpoint.
    private static readonly ConcurrentDictionary<MemberInfo, bool> _compensatableAttrCache = new();

    private static bool HasCompensatableAttribute(MemberInfo member)
        => _compensatableAttrCache.GetOrAdd(member, static m => m.GetCustomAttribute<CompensatableApiAttribute>(inherit: true) is not null);

    private const string LocalOrchestrationKey = "BiUM.Compensation.LocalOrchestration";

    // RequestTransactionMiddleware, next() dönüşünden sonra dispatch edilecek compensation session id'yi
    // buradan okur — ICorrelationContextAccessor'dan DEĞİL. Sebep: CorrelationContextAccessor, AsyncLocal
    // holder'ını her Set çağrısında önce null'layıp sonra yeni bir holder'a geçiyor (HttpContextAccessor'daki
    // gibi). Bu, bu filter'ın (next() içindeki bir descendant) yeni session id'yi set etmesinin, next()'i
    // çağıran middleware'in (ancestor) AsyncLocal pointer'ının hâlâ işaret ettiği ESKİ holder'ı null'laması
    // anlamına gelir — middleware next() dönünce CorrelationContext.CompensationSessionId'i hep null okur,
    // event her zaman buffer'lanır ama asla dispatch edilmez. HttpContext.Items, bu indirection'dan bağımsız
    // düz bir per-request dictionary olduğu için bu sorunu yaşamaz.
    internal const string CompensationSessionIdItemsKey = "BiUM.Compensation.SessionId";

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
            HasCompensatableAttribute(cad.MethodInfo) ||
            HasCompensatableAttribute(cad.ControllerTypeInfo);

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
                    context.HttpContext.Items[CompensationSessionIdItemsKey] = newSession;
                    localOrchestration = true;
                }
            }
        }

        // Custom endpoint [CompensatableApi] varsa ve dışarıdan session gelmemişse
        // API kendi session'ını başlatır ve finalizer olur.
        if (!isCrudMutation && incomingWasEmpty && ctx is not null)
        {
            var newSession = Guid.NewGuid();
            _correlationContextAccessor.CorrelationContext = ctx.WithCompensationSessionId(newSession);
            context.HttpContext.Items[CompensationSessionIdItemsKey] = newSession;
            localOrchestration = true;
        }

        context.HttpContext.Items[LocalOrchestrationKey] = localOrchestration;

        if (!localOrchestration)
        {
            await next();

            return;
        }

        Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext? executedContext = null;

        try
        {
            executedContext = await next();
        }
        catch
        {
            await TryFinalizeAsync(rollback: true, context.HttpContext.RequestAborted);

            throw;
        }

        await TryFinalizeAsync(rollback: IsFailureResult(executedContext?.Result), context.HttpContext.RequestAborted);
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