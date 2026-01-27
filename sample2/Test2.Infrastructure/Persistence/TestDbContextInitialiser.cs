using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace BiApp.Test2.Infrastructure.Persistence;

public partial class DomainBoltDbContextInitialiser : BoltDbContextInitialiser<BoltDbContext, TestDbContext>
{
    public DomainBoltDbContextInitialiser(ILogger<DomainBoltDbContextInitialiser> logger, IOptions<BoltOptions> boltOptions, BoltDbContext boltContext, TestDbContext dbContext)
        : base(logger, boltOptions, boltContext, dbContext)
    {
    }
}

public partial class TestDbContextInitialiser : DbContextInitialiser<TestDbContext>
{
    public TestDbContextInitialiser(ILogger<TestDbContextInitialiser> logger, TestDbContext dbContext) : base(dbContext, logger)
    {
    }

    public override async Task SeedAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
