using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

internal sealed class ApiResponseLoggingFilter(ILogger<ApiResponseLoggingFilter> logger) : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Result is not ObjectResult { Value: ApiResponse response })
        {
            return;
        }

        var path = context.HttpContext.Request.Path.Value ?? string.Empty;

        foreach (var message in response.Messages)
        {
            switch (message.Severity)
            {
                case MessageSeverity.Warning:
                    logger.LogWarning("{Path} API response message [{Code}] {Message}, {Exception}", path, message.Code, message.Message, message.Exception);
                    break;
                case MessageSeverity.Error:
                    logger.LogError("{Path} API response message [{Code}] {Message}, {Exception}", path, message.Code, message.Message, message.Exception);
                    break;
                case MessageSeverity.Information:
                default:
                    logger.LogInformation("{Path} API response message [{Code}] {Message}, {Exception}", path, message.Code, message.Message, message.Exception);
                    break;
            }
        }
    }
}