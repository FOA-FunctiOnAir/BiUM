using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class GeneratorEntityPartialUpdateAttribute : Attribute
{
    public string[] Properties { get; }
    public string? MethodName { get; set; }

    public GeneratorEntityPartialUpdateAttribute(params string[] properties)
    {
        Properties = properties;
    }
}