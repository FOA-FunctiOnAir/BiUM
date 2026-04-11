using BiUM.Contract.Models.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace BiUM.Specialized.Common.API;

internal sealed class ApiResponseTransactionRollbackFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult &&
            objectResult.Value is ApiResponse response &&
            !response.Success)
        {
            var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;

            throw new ApiResponseRollbackException(response, statusCode);
        }

        return next();
    }
}