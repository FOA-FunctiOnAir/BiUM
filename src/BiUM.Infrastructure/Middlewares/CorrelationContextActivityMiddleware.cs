using BiUM.Core.Authorization;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection.Middlewares;

public class CorrelationContextActivityMiddleware
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
                _ = activity.SetTag("correlation.id", correlationContext.CorrelationId);
                _ = activity.SetTag("tenant.id", correlationContext.TenantId);
                _ = activity.SetTag("application.id", correlationContext.ApplicationId);
                _ = activity.SetTag("language.id", correlationContext.LanguageId);

                if (correlationContext.User is not null)
                {
                    _ = activity.SetTag("user.id", correlationContext.User.Id);
                    _ = activity.SetTag("user.identity", correlationContext.User.Identity);
                }
            }
        }

        return _next.Invoke(context);
    }
}
