using System;

namespace BiUM.Core.Common.Exceptions;

public class ApplicationStartupException(string key) : Exception($"Application was not started because of ({key}).")
{
}