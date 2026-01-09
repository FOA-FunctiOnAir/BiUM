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

    SaveCommandRepository = 1 << 8,
    DeleteCommandRepository = 1 << 9,
    UpdateBoltCommandRepository = 1 << 10,

    EntityCommandRepositories =
        SaveCommandRepository |
        DeleteCommandRepository |
        UpdateBoltCommandRepository,

    GetQuery = 1 << 11,
    GetListQuery = 1 << 12,
    GetForNamesQuery = 1 << 13,
    GetForParameterQuery = 1 << 14,

    EntityQueries =
        GetQuery |
        GetListQuery |
        GetForNamesQuery |
        GetForParameterQuery,

    GetQueryRepository = 1 << 15,
    GetListQueryRepository = 1 << 16,
    GetForNamesQueryRepository = 1 << 17,
    GetForParameterQueryRepository = 1 << 18,

    EntityQueryRepositories =
        GetQueryRepository |
        GetListQueryRepository |
        GetForNamesQueryRepository |
        GetForParameterQueryRepository
}
