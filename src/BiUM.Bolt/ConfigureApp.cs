using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ConfigureApp
{
    public static async Task SyncBolt(this IServiceProvider serviceProvider)
    {
        var biAppOptions = serviceProvider.GetRequiredService<IOptions<BiAppOptions>>();
        var boltOptions = serviceProvider.GetRequiredService<IOptions<BoltOptions>>();

        if (!boltOptions.Value.Enable)
        {
            return;
        }

        var initialiser = serviceProvider.GetRequiredService<IBoltDbContextInitialiser>();

        if (biAppOptions.Value.Environment is "Development" or "Production")
        {
            await initialiser.InitialiseAsync();

            await initialiser.SeedAsync();
        }

        await initialiser.EqualizeAsync();
    }
}