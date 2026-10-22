using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ServiceCatalogAttribute : Attribute
{
    public bool Enabled { get; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public ServiceCatalogAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}