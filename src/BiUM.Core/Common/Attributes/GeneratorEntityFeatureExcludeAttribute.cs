using BiUM.Core.Common.Enums;
using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorEntityFeatureExcludeAttribute : Attribute
{
    public IReadOnlyCollection<EntityFeatures> Features { get; }

    public GeneratorEntityFeatureExcludeAttribute()
    {
        Features = [EntityFeatures.All];
    }

    public GeneratorEntityFeatureExcludeAttribute(params EntityFeatures[] features)
    {
        Features = features?.Length > 0 ? features : [EntityFeatures.All];
    }
}