using BiUM.Core.Common.Exceptions;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security;

namespace BiUM.Infrastructure.MagicOnion;

public static class ExceptionExtensions
{
    public static StatusCode ToGrpcStatusCode(this Exception exception)
    {
        if (exception is RpcException rpcException)
        {
            return rpcException.StatusCode;
        }

        return exception switch
        {
            ArgumentNullException => StatusCode.InvalidArgument,
            ArgumentOutOfRangeException => StatusCode.OutOfRange,
            ArgumentException => StatusCode.InvalidArgument,
            ValidationException => StatusCode.InvalidArgument,
            BadHttpRequestException => StatusCode.InvalidArgument,
            FormatException => StatusCode.InvalidArgument,

            InvalidOperationException => StatusCode.FailedPrecondition,

            NullReferenceException => StatusCode.Internal,

            UnauthorizedAccessException => StatusCode.Unauthenticated,
            SecurityException => StatusCode.PermissionDenied,

            NotFoundException => StatusCode.NotFound,
            KeyNotFoundException => StatusCode.NotFound,
            FileNotFoundException => StatusCode.NotFound,
            DirectoryNotFoundException => StatusCode.NotFound,

            TimeoutException => StatusCode.DeadlineExceeded,

            NotImplementedException => StatusCode.Unimplemented,

            OperationCanceledException => StatusCode.Cancelled,

            IndexOutOfRangeException => StatusCode.OutOfRange,

            DbUpdateConcurrencyException => StatusCode.Aborted,

            _ => StatusCode.Internal
        };
    }
}
