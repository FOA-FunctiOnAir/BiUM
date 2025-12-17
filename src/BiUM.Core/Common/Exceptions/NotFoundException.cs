using System;

namespace BiUM.Core.Common.Exceptions;

public class NotFoundException(string name, string key) : Exception($"Entity \"{name}\" ({key}) was not found.")
{
}