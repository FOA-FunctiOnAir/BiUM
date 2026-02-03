using BiUM.Core.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public abstract class DbContextInitialiser<TDbContext> : IDbContextInitialiser
    where TDbContext : DbContext
{
    protected TDbContext DbContext { get; }
    protected IServiceProvider ServiceProvider { get; }
    protected ILogger<DbContextInitialiser<TDbContext>> Logger { get; }

    protected DbContextInitialiser(TDbContext dbContext, IServiceProvider serviceProvider)
    {
        DbContext = dbContext;
        ServiceProvider = serviceProvider;
        Logger = serviceProvider.GetRequiredService<ILogger<DbContextInitialiser<TDbContext>>>();
    }

    public virtual async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DbContext.Database.EnsureCreatedAsync(cancellationToken);

            if (DbContext.Database.IsSqlServer())
            {
                await DbContext.Database.MigrateAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while initialising the database.");

            var type = ex.GetType();

            throw new ApplicationStartupException(type.FullName ?? type.Name, ex);
        }
    }

    public virtual Task SeedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}