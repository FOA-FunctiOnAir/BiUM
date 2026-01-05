using System;

namespace BiUM.Core.Common.Enums;

[Flags]
public enum EntityFeatures
{
    All = 0,

    EntityAddedEvent = 1 << 0,
    EntityUpdatedEvent = 1 << 1,
    EntityDeletedEvent = 1 << 2,

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
