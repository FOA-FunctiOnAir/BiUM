using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace BiUM.Tests.Helpers;

public sealed class TestBiDbContext : BaseDbContext
{
    public TestBiDbContext(
        IServiceProvider serviceProvider,
        DbContextOptions<TestBiDbContext> options,
        EntitySaveChangesInterceptor entitySaveChangesInterceptor)
        : base(serviceProvider, options, entitySaveChangesInterceptor)
    {
    }
}