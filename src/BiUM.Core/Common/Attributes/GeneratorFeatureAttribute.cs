using BiUM.Core.Common.Enums;
using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorFeatureAttribute : Attribute
{
    public EntityFeatures EntityFeatures { get; }

    public GeneratorFeatureAttribute(EntityFeatures entityFeatures = EntityFeatures.All)
    {
        EntityFeatures = entityFeatures;
    }
}
