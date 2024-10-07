using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Logging;

namespace BiUM.Specialized.Database;

public partial class DbContextInitialiser<TDbContext> : IDbContextInitialiser
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
            _context.Database.EnsureCreated();

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

    public virtual async Task SeedAsync()
    {
    }
}