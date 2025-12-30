using System;

namespace BiUM.Core.Audit;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AuditableAttribute : Attribute
{
    public bool Enabled { get; }

    public AuditableAttribute(bool enabled = true)
    {
        Enabled = enabled;
    }
}
