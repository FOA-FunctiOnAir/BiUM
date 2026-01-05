using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SystemFile = System.IO.File;

namespace BiUM.Core.Extensions;

public static class ConfigurationExtensions
{
        public static void OverrideBiAppLocalServices(this IConfigurationBuilder configurationBuilder)
    {
        var localServicesPath = Path.Combine(AppContext.BaseDirectory, "services.local");

        if (!Path.Exists(localServicesPath))
        {
            localServicesPath = Path.Combine(Directory.GetCurrentDirectory(), "services.local");

            if (!Path.Exists(localServicesPath))
            {
                Console.WriteLine($"[{nameof(OverrideBiAppLocalServices)}] services.local not found, skipping overrides.");

                return;
            }
        }

        var localConfigPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Local.json");

        if (!Path.Exists(localConfigPath))
        {
            localConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Local.json");

             if (!Path.Exists(localConfigPath))
             {
                 Console.WriteLine($"[{nameof(OverrideBiAppLocalServices)}] appsettings.Local.json not found, skipping overrides.");

                 return;
             }
        }

        var overrideKeys = SystemFile.ReadAllLines(localServicesPath)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim().Split('/').First().ToLowerInvariant())
            .ToArray();

        if (overrideKeys.Length == 0)
        {
            return;
        }

        Console.WriteLine($"[{nameof(OverrideBiAppLocalServices)}] found {overrideKeys.Length} services to override: {string.Join(", ", overrideKeys)}");

        var localConfigContent = SystemFile.ReadAllText(localConfigPath);
        var localConfig = JsonSerializer.Deserialize<BiAppConfiguration>(localConfigContent);

        var biGrpcOptionsIsNotNull = localConfig?.BiGrpcOptions?.Domains is not null;
        var httpClientsOptionsIsNotNull = localConfig?.HttpClientsOptions?.Domains is not null;

        if (!biGrpcOptionsIsNotNull && !httpClientsOptionsIsNotNull)
        {
            Console.WriteLine($"[{nameof(OverrideBiAppLocalServices)}] Failed to parse appsettings.Local.json routes.");

            return;
        }

        var overrides = new List<KeyValuePair<string, string?>>();

        foreach (var overrideKey in overrideKeys)
        {
            if (biGrpcOptionsIsNotNull)
            {
                SetOverride(overrideKey, "BiGrpcOptions", localConfig?.BiGrpcOptions?.Domains!);
            }

            if (httpClientsOptionsIsNotNull)
            {
                SetOverride(overrideKey, "HttpClientsOptions", localConfig?.HttpClientsOptions?.Domains!);
            }
        }

        if (overrides.Count > 0)
        {
            configurationBuilder.AddInMemoryCollection(overrides);
        }

        return;

        void SetOverride(string overrideKey, string section, Dictionary<string, string?> overrideValues)
        {
            var key = $"{section}:Domains:{overrideKey}";

            if (!overrideValues.TryGetValue(overrideKey, out var value) || string.IsNullOrEmpty(value))
            {
                return;
            }

            overrides.Add(new KeyValuePair<string, string?>(key, value));

            Console.WriteLine($"[{nameof(OverrideBiAppLocalServices)}] Overriding {key} -> {value}");
        }
    }

    private sealed class BiAppConfiguration
    {
        public BiAppServiceConfiguration? BiGrpcOptions { get; set; }
        public BiAppServiceConfiguration? HttpClientsOptions { get; set; }
    }

    private sealed class BiAppServiceConfiguration
    {
        public Dictionary<string, string>? Domains { get; set; }
    }
}
