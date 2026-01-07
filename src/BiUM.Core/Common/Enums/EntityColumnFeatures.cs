using System;

namespace BiUM.Core.Common.Enums;

[Flags]
public enum EntityColumnFeatures
{
    None = 0,

    EntityTranslation = 1 << 1,

    NotSearchable = 1 << 16,
    NotReturnForList = 1 << 17
}
