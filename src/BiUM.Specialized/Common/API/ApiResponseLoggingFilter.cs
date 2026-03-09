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

        foreach (var message in response.Messages)
        {
            switch (message.Severity)
            {
                case MessageSeverity.Warning:
                    logger.LogWarning("API Response Messages: [{Code}] {Message}, {Exception}", message.Code, message.Message, message.Exception);
                    break;
                case MessageSeverity.Error:
                    logger.LogError("API Response Messages: [{Code}] {Message}, {Exception}", message.Code, message.Message, message.Exception);
                    break;
                case MessageSeverity.Information:
                default:
                    logger.LogInformation("API Response Messages: [{Code}] {Message}, {Exception}", message.Code, message.Message, message.Exception);
                    break;
            }
        }
    }
}