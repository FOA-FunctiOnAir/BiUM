using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Configs;

public class BiGrpcOptions
{
    public const string Name = "BiGrpcOptions";

    public bool Enable { get; set; }

    public Dictionary<string, string>? Domains { get; set; }

    public string GetServiceUrl(string serviceKey)
    {
        return Domains?.TryGetValue(serviceKey, out var url) == true
            ? url
            : throw new InvalidOperationException($"{serviceKey} not found in Domains");
    }
}
