using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BiUM.Test.Infrastructure.Persistence;

public partial class DomainBoltDbContextInitialiser : BoltDbContextInitialiser<BoltDbContext, TestDbContext>
{
    public DomainBoltDbContextInitialiser(ILogger<DomainBoltDbContextInitialiser> logger, IOptions<BoltOptions> boltOptions, BoltDbContext boltContext, TestDbContext context)
        : base(logger, boltOptions, boltContext, context)
    {
    }
}

public partial class TestDbContextInitialiser : DbContextInitialiser<TestDbContext>
{
    public TestDbContextInitialiser(ILogger<TestDbContextInitialiser> logger, TestDbContext context) : base(logger, context)
    {
    }

    public override async Task SeedAsync()
    {
        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}