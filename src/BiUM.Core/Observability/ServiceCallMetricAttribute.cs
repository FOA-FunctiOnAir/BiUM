using System;

namespace BiUM.Core.Observability;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class ServiceCallMetricAttribute : Attribute
{
    public bool Enabled { get; }

    public ServiceCallMetricAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}