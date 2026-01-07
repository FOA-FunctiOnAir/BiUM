using BiUM.Core.Common.Enums;
using System;

namespace BiUM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class GeneratorColumnFeatureAttribute : Attribute
{
    public EntityColumnFeatures EntityColumnFeatures { get; }

    public GeneratorColumnFeatureAttribute(EntityColumnFeatures entityColumnFeatures = EntityColumnFeatures.None)
    {
        EntityColumnFeatures = entityColumnFeatures;
    }
}
