using System;

namespace BiUM.Core.Common.Enums;

[Flags]
public enum EntityFeatures
{
    None = 0,
    All = 1,

    EntityAddedEvent = 1 << 1,
    EntityUpdatedEvent = 1 << 2,
    EntityDeletedEvent = 1 << 3,

    EntityEvents =
        EntityAddedEvent |
        EntityUpdatedEvent |
        EntityDeletedEvent,

    SaveCommand = 1 << 4,
    DeleteCommand = 1 << 5,
    UpdateBoltCommand = 1 << 6,

    EntityCommands =
        SaveCommand |
        DeleteCommand |
        UpdateBoltCommand,

    GetQuery = 1 << 7,
    GetListQuery = 1 << 8,
    GetForNamesQuery = 1 << 9,
    GetForParameterQuery = 1 << 10,

    EntityQueries =
        GetQuery |
        GetListQuery |
        GetForNamesQuery |
        GetForParameterQuery
}
