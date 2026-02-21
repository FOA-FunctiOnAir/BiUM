using BiUM.Core.Common.Enums;
using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorEntityFeatureAttribute : Attribute
{
    public string? MainEntityName { get; }
    public IReadOnlyCollection<EntityFeatures> Features { get; }

    public GeneratorEntityFeatureAttribute()
    {
        MainEntityName = string.Empty;
        Features = [EntityFeatures.All];
    }

    public GeneratorEntityFeatureAttribute(string mainEntityName)
    {
        MainEntityName = mainEntityName;
        Features = [EntityFeatures.All];
    }

    public GeneratorEntityFeatureAttribute(params EntityFeatures[] features)
    {
        Features = features?.Length > 0 ? features : [EntityFeatures.All];
    }

    public GeneratorEntityFeatureAttribute(string mainEntityName, params EntityFeatures[] features)
    {
        MainEntityName = mainEntityName;
        Features = features?.Length > 0 ? features : [EntityFeatures.All];
    }
}