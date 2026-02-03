using BiUM.Bolt.Database;
using BiUM.Core.Common.Configs;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Options;
using System;

namespace BiApp.Test2.Infrastructure.Persistence;

public class DomainBoltDbContextInitialiser : BoltDbContextInitialiser<BoltDbContext, TestDbContext>
{
    public DomainBoltDbContextInitialiser(IServiceProvider serviceProvider, IOptions<BoltOptions> boltOptions, BoltDbContext boltDbContext, TestDbContext dbContext)
        : base(serviceProvider, boltOptions, boltDbContext, dbContext)
    {
    }
}

public class TestDbContextInitialiser : DbContextInitialiser<TestDbContext>
{
    public TestDbContextInitialiser(TestDbContext dbContext, IServiceProvider serviceProvider) : base(dbContext, serviceProvider)
    {
    }
}