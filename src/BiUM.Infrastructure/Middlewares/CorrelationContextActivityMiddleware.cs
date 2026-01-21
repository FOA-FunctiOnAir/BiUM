using BiUM.Core.Authorization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BiUM.Infrastructure.Middlewares;

internal sealed class CorrelationContextActivityMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationContextActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, ICorrelationContextProvider correlationContextProvider)
    {
        var correlationContext = correlationContextProvider.Get();

        if (correlationContext is not null)
        {
            var activity = Activity.Current;

            if (activity is not null)
            {
                activity.SetTag("correlation.id", correlationContext.CorrelationId);
                activity.SetTag("tenant.id", correlationContext.TenantId);
                activity.SetTag("application.id", correlationContext.ApplicationId);
                activity.SetTag("language.id", correlationContext.LanguageId);

                if (correlationContext.User is not null)
                {
                    activity.SetTag("user.id", correlationContext.User.Id);
                    activity.SetTag("user.identity", correlationContext.User.Identity);
                }
            }
        }

        return _next.Invoke(context);
    }
}
