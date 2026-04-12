using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Database;
using BiUM.Specialized.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BiUM.Tests.Transaction;

public sealed class RequestTransactionMiddlewarePoliciesTests
{
    [Theory]
    [InlineData("GET", "/api/x", true)]
    [InlineData("HEAD", "/api/x", true)]
    [InlineData("OPTIONS", "/api/x", true)]
    [InlineData("POST", "/health", true)]
    [InlineData("POST", "/health/ready", true)]
    [InlineData("POST", "/swagger/index.html", true)]
    [InlineData("POST", "/version", true)]
    [InlineData("POST", "/api/save", false)]
    [InlineData("PUT", "/api/x", false)]
    [InlineData("PATCH", "/api/x", false)]
    [InlineData("DELETE", "/api/x", false)]
    public void ShouldSkipTransaction_reflects_method_and_path(string method, string path, bool expectedSkip)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;

        RequestTransactionMiddlewarePolicies.ShouldSkipTransaction(httpContext).Should().Be(expectedSkip);
    }

    [Fact]
    public void ShouldSkipTransaction_true_for_grpc_content_type()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "POST";
        httpContext.Request.Path = "/api/x";
        httpContext.Request.ContentType = "application/grpc";

        RequestTransactionMiddlewarePolicies.ShouldSkipTransaction(httpContext).Should().BeTrue();
    }

    [Fact]
    public void IsInMemoryDatabaseProvider_true_when_ef_in_memory()
    {
        var options = new DbContextOptionsBuilder<TestPolicyDbContext>()
            .UseInMemoryDatabase(nameof(IsInMemoryDatabaseProvider_true_when_ef_in_memory))
            .Options;

        using var ctx = new TestPolicyDbContext(options);

        RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(ctx).Should().BeTrue();
    }

    [Fact]
    public void IsInMemoryDatabaseProvider_false_when_sqlite()
    {
        var options = new DbContextOptionsBuilder<TestPolicyDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var ctx = new TestPolicyDbContext(options);

        RequestTransactionMiddlewarePolicies.IsInMemoryDatabaseProvider(ctx).Should().BeFalse();
    }

    private sealed class TestPolicyDbContext : DbContext, IDbContext
    {
        public TestPolicyDbContext(DbContextOptions<TestPolicyDbContext> options)
            : base(options)
        {
        }

        public DbSet<DomainCrud> DomainCruds => Set<DomainCrud>();
        public DbSet<DomainCrudColumn> DomainCrudColumns => Set<DomainCrudColumn>();
        public DbSet<DomainCrudTranslation> DomainCrudTranslations => Set<DomainCrudTranslation>();
        public DbSet<DomainCrudVersion> DomainCrudVersions => Set<DomainCrudVersion>();
        public DbSet<DomainCrudVersionColumn> DomainCrudVersionColumns => Set<DomainCrudVersionColumn>();
        // public DbSet<DomainDynamicApi> DomainDynamicApis => Set<DomainDynamicApi>();
        // public DbSet<DomainDynamicApiParameter> DomainDynamicApiParameters => Set<DomainDynamicApiParameter>();
        // public DbSet<DomainDynamicApiTranslation> DomainDynamicApiTranslations => Set<DomainDynamicApiTranslation>();
        // public DbSet<DomainDynamicApiVersion> DomainDynamicApiVersions => Set<DomainDynamicApiVersion>();
        // public DbSet<DomainDynamicApiVersionParameter> DomainDynamicApiVersionParameters => Set<DomainDynamicApiVersionParameter>();
        public DbSet<DomainTranslation> DomainTranslations => Set<DomainTranslation>();
        public DbSet<DomainTranslationDetail> DomainTranslationDetails => Set<DomainTranslationDetail>();
        public DbSet<DomainCompensationSnapshot> DomainCompensationSnapshots => Set<DomainCompensationSnapshot>();
        public DbSet<DomainCrudPartialUpdate> DomainCrudPartialUpdates => Set<DomainCrudPartialUpdate>();
        public DbSet<DomainCrudPartialUpdateColumn> DomainCrudPartialUpdateColumns => Set<DomainCrudPartialUpdateColumn>();
        public DbSet<DomainCrudVersionPartialUpdate> DomainCrudVersionPartialUpdates => Set<DomainCrudVersionPartialUpdate>();
        public DbSet<DomainCrudVersionPartialUpdateColumn> DomainCrudVersionPartialUpdateColumns => Set<DomainCrudVersionPartialUpdateColumn>();

        public new Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            base.SaveChangesAsync(cancellationToken);
    }
}