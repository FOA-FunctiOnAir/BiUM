using BiUM.Core.Common.Enums;
using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorEntityFeatureAttribute : Attribute
{
    public IReadOnlyCollection<EntityFeatures> Features { get; }

    public GeneratorEntityFeatureAttribute()
    {
        Features = [EntityFeatures.All];
    }

    public GeneratorEntityFeatureAttribute(params EntityFeatures[] features)
    {
        Features = features?.Length > 0 ? features : [EntityFeatures.All];
    }
}
