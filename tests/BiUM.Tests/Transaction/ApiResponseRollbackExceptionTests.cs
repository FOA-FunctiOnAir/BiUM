using BiUM.Contract.Enums;
using BiUM.Contract.Models.Api;
using FluentAssertions;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class ApiResponseRollbackExceptionTests
{
    [Fact]
    public void Constructor_preserves_api_response_and_default_status()
    {
        var response = new ApiResponse();
        response.AddMessage(new ResponseMessage { Code = "c", Message = "m", Severity = MessageSeverity.Error });

        var ex = new ApiResponseRollbackException(response);

        ex.ApiResponse.Should().BeSameAs(response);
        ex.StatusCode.Should().Be(200);
        ex.Message.Should().Contain("Transaction rollback");
    }

    [Fact]
    public void Constructor_accepts_custom_status_code()
    {
        var response = new ApiResponse();
        response.AddMessage(new ResponseMessage { Code = "c", Message = "m", Severity = MessageSeverity.Error });

        var ex = new ApiResponseRollbackException(response, 422);

        ex.StatusCode.Should().Be(422);
    }
}