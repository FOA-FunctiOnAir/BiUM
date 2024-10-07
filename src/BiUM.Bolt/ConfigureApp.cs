using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static async Task SyncBolt(this IServiceScope scope)
    {
        var biAppOptions = scope.ServiceProvider.GetRequiredService<IOptions<BiAppOptions>>();
        var boltOptions = scope.ServiceProvider.GetRequiredService<IOptions<BoltOptions>>();

        if (boltOptions?.Value?.Enable != null && boltOptions.Value.Enable)
        {
            var initialiser = scope.ServiceProvider.GetRequiredService<IBoltDbContextInitialiser>();

            if (biAppOptions?.Value != null && biAppOptions.Value.Environment == "Development")
            {
                await initialiser.InitialiseAsync();
                await initialiser.SeedAsync();
            }

            await initialiser.EqualizeAsync();
        }
    }
}