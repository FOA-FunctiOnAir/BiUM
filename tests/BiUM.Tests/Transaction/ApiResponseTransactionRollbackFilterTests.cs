using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using BiUM.Specialized.Common.API;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class ApiResponseTransactionRollbackFilterTests
{
    [Fact]
    public async Task OnResultExecutionAsync_failure_ObjectResult_throws_with_status_and_response()
    {
        var filter = new ApiResponseTransactionRollbackFilter();

        var apiResponse = new ApiResponse();
        apiResponse.AddMessage(new ResponseMessage { Code = "e", Message = "err", Severity = MessageSeverity.Error });

        var objectResult = new ObjectResult(apiResponse) { StatusCode = 422 };
        var executingContext = CreateResultExecutingContext(objectResult);

        var ex = await Assert.ThrowsAsync<ApiResponseRollbackException>(() =>
            filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(new ResultExecutedContext(
                executingContext,
                executingContext.Filters,
                executingContext.Result,
                executingContext.Controller))));

        ex.StatusCode.Should().Be(422);
        ex.ApiResponse.Should().BeSameAs(apiResponse);
    }

    [Fact]
    public async Task OnResultExecutionAsync_success_invokes_next()
    {
        var filter = new ApiResponseTransactionRollbackFilter();

        var apiResponse = new ApiResponse();
        var objectResult = new ObjectResult(apiResponse) { StatusCode = 200 };
        var executingContext = CreateResultExecutingContext(objectResult);

        var nextCalled = false;
        await filter.OnResultExecutionAsync(executingContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ResultExecutedContext(
                executingContext,
                executingContext.Filters,
                executingContext.Result,
                executingContext.Controller));
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OnResultExecutionAsync_ApiResponse_of_T_inherits_failure()
    {
        var filter = new ApiResponseTransactionRollbackFilter();

        var apiResponse = new ApiResponse<string>("x");
        apiResponse.AddMessage(new ResponseMessage { Code = "e", Message = "err", Severity = MessageSeverity.Error });

        var objectResult = new ObjectResult(apiResponse);
        var executingContext = CreateResultExecutingContext(objectResult);

        await Assert.ThrowsAsync<ApiResponseRollbackException>(() =>
            filter.OnResultExecutionAsync(executingContext, () => Task.FromResult(new ResultExecutedContext(
                executingContext,
                executingContext.Filters,
                executingContext.Result,
                executingContext.Controller))));
    }

    private static ResultExecutingContext CreateResultExecutingContext(IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(actionContext, [], result, controller: new object());
    }
}