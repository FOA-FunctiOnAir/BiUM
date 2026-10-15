using System;
using System.Collections.Generic;

namespace BiUM.Core.Common.Configs;

public class BiGrpcOptions
{
    public const string Name = "BiGrpcOptions";

    public Dictionary<string, string>? Domains { get; set; }

    public string GetServiceUrl(string serviceKey)
    {
        var normalizedKey = serviceKey.Replace('.', '-');

        return Domains?.TryGetValue(normalizedKey, out var url) == true
            ? url
            : throw new InvalidOperationException($"{normalizedKey} not found in Domains");
    }
}