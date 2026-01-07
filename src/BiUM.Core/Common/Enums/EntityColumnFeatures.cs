using System;

namespace BiUM.Core.Common.Enums;

[Flags]
public enum EntityColumnFeatures
{
    None = 0,

    EntityTranslation = 1,

    NotSearchable = 101,
    NotReturnForList = 102
}
