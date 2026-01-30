using BiUM.Core.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security;

namespace System;

public static partial class Extensions
{
    public static int ToStatusCode(this Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => StatusCodes.Status400BadRequest,
            ArgumentOutOfRangeException => StatusCodes.Status400BadRequest,
            ArgumentException => StatusCodes.Status400BadRequest,
            ValidationException => StatusCodes.Status400BadRequest,
            BadHttpRequestException => StatusCodes.Status400BadRequest,
            FormatException => StatusCodes.Status400BadRequest,

            InvalidOperationException => StatusCodes.Status400BadRequest,

            NullReferenceException => StatusCodes.Status500InternalServerError,

            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            SecurityException => StatusCodes.Status403Forbidden,

            NotFoundException => StatusCodes.Status404NotFound,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            FileNotFoundException => StatusCodes.Status404NotFound,
            DirectoryNotFoundException => StatusCodes.Status404NotFound,

            TimeoutException => StatusCodes.Status408RequestTimeout,

            NotImplementedException => StatusCodes.Status501NotImplemented,

            DbUpdateConcurrencyException => StatusCodes.Status500InternalServerError,

            OperationCanceledException => StatusCodes.Status500InternalServerError,

            IndexOutOfRangeException => StatusCodes.Status400BadRequest,

            ApplicationStartupException => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status500InternalServerError
        };
    }
}