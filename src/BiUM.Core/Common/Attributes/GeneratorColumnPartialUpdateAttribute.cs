using System;

namespace BiUM.Core.Common.Attributes;


[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class GeneratorColumnPartialUpdateAttribute : Attribute
{
    public string[]? IncludeProperties { get; set; }
}