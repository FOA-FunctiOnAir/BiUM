using System;

namespace BiUM.Specialized.Services.DynamicApi;

internal interface IDynamicApiRuntimeInternals
{
    Guid DynamicApiId { get; }

    string ConnectionString { get; }

    string DatabaseType { get; }
}