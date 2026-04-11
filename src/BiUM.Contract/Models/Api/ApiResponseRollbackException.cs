using System;

namespace BiUM.Contract.Models.Api;

public sealed class ApiResponseRollbackException : Exception
{
    public ApiResponse ApiResponse { get; }

    public int StatusCode { get; }

    public ApiResponseRollbackException(ApiResponse apiResponse, int statusCode = 200)
        : base("Transaction rollback requested due to API response error messages.")
    {
        ApiResponse = apiResponse;
        StatusCode = statusCode;
    }
}