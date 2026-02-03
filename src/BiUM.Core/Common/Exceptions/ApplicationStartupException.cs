using System;

namespace BiUM.Core.Common.Exceptions;

public class ApplicationStartupException : Exception
{
    public ApplicationStartupException(string message) :
        base($"Application was not started because of ({message}).")
    {
    }

    public ApplicationStartupException(string message, Exception innerException) :
        base($"Application was not started because of ({message}).", innerException)
    {
    }
}