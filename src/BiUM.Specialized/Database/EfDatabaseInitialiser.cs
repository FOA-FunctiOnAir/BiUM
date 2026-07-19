using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public static class EfDatabaseInitialiser
{
    public static async Task InitialiseSchemaAsync(
        DbContext dbContext,
        IConfiguration configuration,
        bool bolt,
        CancellationToken cancellationToken = default)
    {
        if (EfMigrationsAssemblyResolver.ShouldUseMigrations(configuration, bolt))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
    }
}