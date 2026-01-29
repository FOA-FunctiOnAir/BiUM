using BiUM.Core.Common.Enums;
using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public class GeneratorColumnFeatureAttribute : Attribute
{
    public IReadOnlyCollection<EntityColumnFeatures> Features { get; }


    public GeneratorColumnFeatureAttribute()
    {
        Features = [EntityColumnFeatures.None];
    }

    public GeneratorColumnFeatureAttribute(params EntityColumnFeatures[] features)
    {
        Features = features?.Length > 0 ? features : [EntityColumnFeatures.None];
    }
}