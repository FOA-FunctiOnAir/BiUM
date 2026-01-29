namespace BiUM.Core.Common.Enums;

public enum EntityFeatures
{
    None = 0,
    All = 1,

    EntityAddedEvent = 2,
    EntityUpdatedEvent = 3,
    EntityDeletedEvent = 4,

    EntityEvents = 49,

    Save = 50, // Controller, Command, Repository
    Delete = 51, // Controller, Command, Repository
    UpdateBolt = 52, // Controller, Command, Repository
    UpdatePartial = 53, // Controller, Command, Repository

    EntityCommandControllers = 99,  // All Controllers, All Commands, All Repositories

    SaveCommandAndRepository = 100,  // Command, Repository
    DeleteCommandAndRepository = 101,  // Command, Repository
    UpdateBoltCommandAndRepository = 102,  // Command, Repository
    UpdatePartialCommandAndRepository = 103,  // Command, Repository

    EntityCommandsAndRepositories = 149,  // All Commands, All Repositories

    SaveCommand = 150,  // Command
    DeleteCommand = 151,  // Command
    UpdateBoltCommand = 152,  // Command
    UpdatePartialCommand = 153,  // Command

    EntityCommands = 199,  // All Commands, All Repository

    SaveRepository = 200, // Repository
    DeleteRepository = 201, // Repository
    UpdateBoltRepository = 202, // Repository
    UpdatePartialRepository = 203, // Repository

    EntityCommandRepositories = 249, // All Command Repository

    Get = 250, // Controller, Query, Repository
    GetList = 251, // Controller, Query, Repository
    GetForNames = 252, // Controller, Query, Repository
    GetForParameter = 253, // Controller, Query, Repository

    EntityQueryControllers = 299,  // All Controllers, All Queries, All Repositories

    GetQueryAndRepository = 300, // Query, Repository
    GetListQueryAndRepository = 301, // Query, Repository
    GetForNamesQueryAndRepository = 302, // Query, Repository
    GetForParameterQueryAndRepository = 303, // Query, Repository

    EntityQueriesAndRepositories = 349,  // All Queries, All Repositories

    GetQuery = 350, // Query
    GetListQuery = 351, // Query
    GetForNamesQuery = 352, // Query
    GetForParameterQuery = 353, // Query

    EntityQueries = 399, // All Query, All Repository

    GetRepository = 400, // Repository
    GetListRepository = 401, // Repository
    GetForNamesRepository = 402, // Repository
    GetForParameterRepository = 403, // Repository

    EntityQueryRepositories = 449 // All Queries Repository
}