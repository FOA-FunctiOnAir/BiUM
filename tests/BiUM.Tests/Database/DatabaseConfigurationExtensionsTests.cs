using BiUM.Core.Common.Configs;
using BiUM.Core.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BiUM.Tests.Database;

public sealed class DatabaseConfigurationExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void GetDatabaseOptions_reads_default_section()
    {
        var config = BuildConfig(new()
        {
            ["Databases:Default:Type"] = "MSSQL",
            ["Databases:Default:ConnectionString"] = "Server=main"
        });

        var options = config.GetDatabaseOptions();

        options.Type.Should().Be(DatabaseTypes.MSSQL);
        options.ConnectionString.Should().Be("Server=main");
    }

    [Fact]
    public void GetDatabaseOptions_reads_named_section_independently()
    {
        var config = BuildConfig(new()
        {
            ["Databases:Default:ConnectionString"] = "Host=main",
            ["Databases:Readings:Type"] = "PostgreSQL",
            ["Databases:Readings:ConnectionString"] = "Host=readings"
        });

        config.GetDatabaseOptions("Readings").ConnectionString.Should().Be("Host=readings");
        config.GetDatabaseOptions().ConnectionString.Should().Be("Host=main");
    }

    [Fact]
    public void GetDatabaseType_defaults_to_postgresql_when_type_is_missing()
    {
        var config = BuildConfig(new()
        {
            ["Databases:Default:ConnectionString"] = "Host=main"
        });

        config.GetDatabaseType().Should().Be(DatabaseTypes.PostgreSQL);
    }

    [Fact]
    public void GetDatabaseOptions_ignores_legacy_root_keys()
    {
        var config = BuildConfig(new()
        {
            ["DatabaseType"] = "MSSQL",
            ["ConnectionStrings:MSSQL"] = "Server=legacy"
        });

        var options = config.GetDatabaseOptions();

        options.Type.Should().Be(DatabaseTypes.PostgreSQL);
        options.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void GetRequiredDatabaseConnectionString_throws_with_section_path_when_missing()
    {
        var config = BuildConfig(new());

        var act = () => config.GetRequiredDatabaseConnectionString("Readings");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Databases:Readings:ConnectionString*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void GetDatabaseOptions_rejects_empty_name(string? name)
    {
        var config = BuildConfig(new());

        var act = () => config.GetDatabaseOptions(name!);

        act.Should().Throw<ArgumentException>();
    }
}