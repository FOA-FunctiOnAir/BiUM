using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BiUM.Tests.Database;

public class EfMigrationsAssemblyResolverTests
{
    private static IConfiguration BuildConfig(string? domain = "Customers", string? databaseType = "PostgreSQL") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BiAppOptions:Domain"] = domain,
                ["DatabaseType"] = databaseType
            })
            .Build();

    [Theory]
    [InlineData("Customers", "PostgreSQL", false, "BiApp.Customers.Migrations.Postgres")]
    [InlineData("Customers", "PostgreSQL", true, "BiApp.Customers.Migrations.Postgres.Bolt")]
    [InlineData("Customers", "MSSQL", false, "BiApp.Customers.Migrations.Mssql")]
    [InlineData("Customers", "MSSQL", true, "BiApp.Customers.Migrations.Mssql.Bolt")]
    [InlineData("Sales", "PostgreSQL", false, "BiApp.Sales.Migrations.Postgres")]
    public void GetMigrationsAssemblyName_returns_correct_name(
        string domain, string databaseType, bool bolt, string expected)
    {
        var config = BuildConfig(domain, databaseType);

        var result = EfMigrationsAssemblyResolver.GetMigrationsAssemblyName(config, bolt);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "PostgreSQL")]
    [InlineData("", "PostgreSQL")]
    [InlineData("Customers", null)]
    [InlineData("Customers", "")]
    [InlineData("Customers", "InMemory")]
    [InlineData("Customers", "SQLite")]
    public void GetMigrationsAssemblyName_returns_null_for_invalid_config(string? domain, string? databaseType)
    {
        var config = BuildConfig(domain, databaseType);

        var result = EfMigrationsAssemblyResolver.GetMigrationsAssemblyName(config, bolt: false);

        Assert.Null(result);
    }

    [Fact]
    public void ShouldUseMigrations_returns_false_when_assembly_not_loaded()
    {
        var config = BuildConfig("NonExistentDomain", "PostgreSQL");

        var result = EfMigrationsAssemblyResolver.ShouldUseMigrations(config, bolt: false);

        Assert.False(result);
    }

    [Fact]
    public void GetActiveMigrationsAssemblyName_returns_null_when_assembly_not_loaded()
    {
        var config = BuildConfig("NonExistentDomain", "PostgreSQL");

        var result = EfMigrationsAssemblyResolver.GetActiveMigrationsAssemblyName(config, bolt: false);

        Assert.Null(result);
    }

    [Fact]
    public void ShouldUseMigrations_returns_false_for_InMemory()
    {
        var config = BuildConfig("Customers", "InMemory");

        var result = EfMigrationsAssemblyResolver.ShouldUseMigrations(config, bolt: false);

        Assert.False(result);
    }
}