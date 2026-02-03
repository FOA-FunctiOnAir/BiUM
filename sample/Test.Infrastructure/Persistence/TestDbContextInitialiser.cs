using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Options;
using System;

namespace BiApp.Test.Infrastructure.Persistence;

public class DomainBoltDbContextInitialiser : BoltDbContextInitialiser<BoltDbContext, TestDbContext>
{
    public DomainBoltDbContextInitialiser(IServiceProvider serviceProvider, IOptions<BoltOptions> boltOptions, BoltDbContext boltContext, TestDbContext dbContext)
        : base(serviceProvider, boltOptions, boltContext, dbContext)
    {
    }
}

public class TestDbContextInitialiser : DbContextInitialiser<TestDbContext>
{
    public TestDbContextInitialiser(TestDbContext dbContext, IServiceProvider serviceProvider) : base(dbContext, serviceProvider)
    {
    }
}