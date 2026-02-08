using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class EncryptedDataAttribute : Attribute
{
    public bool Reversible { get; }

    public EncryptedDataAttribute(bool reversible = false)
    {
        Reversible = reversible;
    }
}