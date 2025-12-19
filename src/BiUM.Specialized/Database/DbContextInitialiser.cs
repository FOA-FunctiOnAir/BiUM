using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BiUM.Specialized.Database;

public class DbContextInitialiser<TDbContext> : IDbContextInitialiser
    where TDbContext : DbContext
{
    public readonly ILogger<DbContextInitialiser<TDbContext>> _logger;
    public readonly TDbContext _context;

    public DbContextInitialiser(ILogger<DbContextInitialiser<TDbContext>> logger, TDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public virtual async Task InitialiseAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();

            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public virtual Task SeedAsync()
    {
        return Task.CompletedTask;
    }
}