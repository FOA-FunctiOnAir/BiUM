using System.Collections.Generic;

namespace BiUM.Core.Constants;

public static class BiAppEnvironments
{
    public const string Development = nameof(Development);
    public const string PreDevelopment = nameof(PreDevelopment);
    public const string Production = nameof(Production);
    public const string Sandbox = nameof(Sandbox);

    public static readonly IReadOnlySet<string> All
        = new HashSet<string>
        {
            Development,
            PreDevelopment,
            Production,
            Sandbox
        };
}