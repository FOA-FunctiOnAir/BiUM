namespace BiUM.Core.Common.Enums;

public enum EntityFeatures
{
    None = 0,
    All = 1,

    EntityAddedEvent = 2,
    EntityUpdatedEvent = 3,
    EntityDeletedEvent = 4,

    EntityEvents = 9,

    Save = 50, // Controller, Command, Repository
    Delete = 51, // Controller, Command, Repository
    UpdateBolt = 52, // Controller, Command, Repository
    UpdatePartial = 53, // Controller, Command, Repository

    SaveCommand = 60,  // Command, Repository
    DeleteCommand = 61,  // Command, Repository
    UpdateBoltCommand = 62,  // Command, Repository
    UpdatePartialCommand = 63,  // Command, Repository

    EntityCommands = 69,  // All Commands, All Repository

    SaveRepository = 70, // Repository
    DeleteRepository = 71, // Repository
    UpdateBoltRepository = 72, // Repository
    UpdatePartialRepository = 73, // Repository

    EntityRepositories = 79, // All Repository

    Get = 80, // Controller, Query, Repository
    GetList = 81, // Controller, Query, Repository
    GetForNames = 82, // Controller, Query, Repository
    GetForParameter = 83, // Controller, Query, Repository

    GetQuery = 90, // Query, Repository
    GetListQuery = 91, // Query, Repository
    GetForNamesQuery = 92, // Query, Repository
    GetForParameterQuery = 93, // Query, Repository

    EntityQueries = 99, // All Query, All Repository

    GetRepository = 100, // Repository
    GetListRepository = 101, // Repository
    GetForNamesRepository = 102, // Repository
    GetForParameterRepository = 103, // Repository

    EntityQueryRepositories = 109 // All Repository
}
