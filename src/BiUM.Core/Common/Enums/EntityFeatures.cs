using System;

namespace BiUM.Core.Common.Enums;

[Flags]
public enum EntityFeatures
{
    None = 0,
    All = 1,

    EntityAddedEvent = 1 << 2,
    EntityUpdatedEvent = 1 << 3,
    EntityDeletedEvent = 1 << 4,

    EntityEvents =
        EntityAddedEvent |
        EntityUpdatedEvent |
        EntityDeletedEvent,

    SaveCommand = 1 << 5,
    DeleteCommand = 1 << 6,
    UpdateBoltCommand = 1 << 7,

    EntityCommands =
        SaveCommand |
        DeleteCommand |
        UpdateBoltCommand,

    GetQuery = 1 << 8,
    GetListQuery = 1 << 9,
    GetForNamesQuery = 1 << 10,
    GetForParameterQuery = 1 << 11,

    EntityQueries =
        GetQuery |
        GetListQuery |
        GetForNamesQuery |
        GetForParameterQuery
}
