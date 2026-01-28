using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public abstract class DbContextInitialiser<TDbContext> : IDbContextInitialiser
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; }
    protected ILogger<DbContextInitialiser<TDbContext>> Logger { get; }

    protected DbContextInitialiser(TDbContext dbContext, ILogger<DbContextInitialiser<TDbContext>> logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    public virtual async Task InitialiseAsync()
    {
        try
        {
            await DbContext.Database.EnsureCreatedAsync();

            if (DbContext.Database.IsSqlServer())
            {
                await DbContext.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while initialising the database.");

            throw;
        }
    }

    public virtual Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}
