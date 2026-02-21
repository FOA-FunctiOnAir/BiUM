using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorEntityGroupFeatureAttribute : Attribute
{
    public string? MainEntityName { get; }

    public GeneratorEntityGroupFeatureAttribute(string mainEntityName)
    {
        MainEntityName = mainEntityName;
    }
}