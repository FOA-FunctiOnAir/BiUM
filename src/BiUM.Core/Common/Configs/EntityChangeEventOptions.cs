using System.Collections.Generic;

namespace BiUM.Core.Common.Configs;

public class EntityChangeEventOptions
{
    public const string Name = "EntityChangeEvents";

    public bool Enable { get; set; } = true;

    public List<string> ExcludedTables { get; set; } = [];
}