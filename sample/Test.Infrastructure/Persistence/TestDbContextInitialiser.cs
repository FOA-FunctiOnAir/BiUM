using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.Persistence;

public partial class DomainBoltDbContextInitialiser : BoltDbContextInitialiser<BoltDbContext, TestDbContext>
{
    public DomainBoltDbContextInitialiser(IServiceProvider serviceProvider, IOptions<BoltOptions> boltOptions, BoltDbContext boltContext, TestDbContext dbContext)
        : base(serviceProvider, boltOptions, boltContext, dbContext)
    {
    }
}

public partial class TestDbContextInitialiser : DbContextInitialiser<TestDbContext>
{
    public TestDbContextInitialiser(TestDbContext dbContext, IServiceProvider serviceProvider) : base(dbContext, serviceProvider)
    {
    }

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
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