using BiUM.Contract.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

internal sealed class ApiResponseTransactionRollbackFilter : IAsyncResultFilter
{
    internal const string RollbackRequestedKey = "BiUM.Transaction.RollbackRequested";

    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: ApiResponse response } && !response.Success)
        {
            context.HttpContext.Items[RollbackRequestedKey] = true;
        }

        return next();
    }
}