using BiUM.Core.Database.Native;
using BiUM.Specialized.Database.Native;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BiUM.Tests.Database.Native;

public sealed class NativeDatabaseRegistrationTests
{
    private const string DatabaseName = "TestNative";

    private static IConfiguration BuildConfig(string type = "PostgreSQL", string? connectionString = "Host=localhost; Port=5432; Database=test_native; Username=u; Password=p;") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Databases:{DatabaseName}:Type"] = type,
                [$"Databases:{DatabaseName}:ConnectionString"] = connectionString
            })
            .Build();

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));

        return services;
    }

    [Fact]
    public void AddBiUMNativeDatabase_registers_keyed_native_database_without_connecting()
    {
        var services = CreateServices();

        services.AddBiUMNativeDatabase(BuildConfig(), DatabaseName);

        using var provider = services.BuildServiceProvider();

        var database = provider.GetRequiredKeyedService<INativeDatabase>(DatabaseName);

        database.Name.Should().Be(DatabaseName);
        provider.GetKeyedService<INativeDatabase>("Other").Should().BeNull();
    }

    [Fact]
    public void AddBiUMNativeDatabase_collects_only_migrations_of_the_same_database()
    {
        var services = CreateServices();

        services.AddBiUMNativeDatabase(BuildConfig(), DatabaseName, typeof(NativeDatabaseRegistrationTests).Assembly);

        using var provider = services.BuildServiceProvider();

        var migrations = provider.GetServices<NativeSchemaMigration>().ToList();

        migrations.Should().ContainSingle(migration => migration is TestNativeMigration);
        migrations.Should().NotContain(migration => migration is OtherDatabaseMigration);
    }

    [Fact]
    public void AddBiUMNativeDatabase_is_idempotent_for_the_same_name()
    {
        var services = CreateServices();

        services.AddBiUMNativeDatabase(BuildConfig(), DatabaseName, typeof(NativeDatabaseRegistrationTests).Assembly);
        services.AddBiUMNativeDatabase(BuildConfig(), DatabaseName, typeof(NativeDatabaseRegistrationTests).Assembly);

        services.Count(descriptor => descriptor.ServiceType == typeof(INativeDatabase)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(NativeSchemaMigration)).Should().Be(1);
        services.Count(descriptor => descriptor.ServiceType == typeof(NativeDatabaseInitialiser)).Should().Be(1);
    }

    [Theory]
    [InlineData("MSSQL")]
    [InlineData("InMemory")]
    public void AddBiUMNativeDatabase_rejects_non_postgresql_types(string type)
    {
        var services = CreateServices();

        var act = () => services.AddBiUMNativeDatabase(BuildConfig(type), DatabaseName);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void AddBiUMNativeDatabase_requires_connection_string()
    {
        var services = CreateServices();

        var act = () => services.AddBiUMNativeDatabase(BuildConfig(connectionString: null), DatabaseName);

        act.Should().Throw<InvalidOperationException>().WithMessage($"*Databases:{DatabaseName}:ConnectionString*");
    }

    [Fact]
    public async Task InitialiseNativeDatabases_is_noop_without_registrations()
    {
        using var provider = CreateServices().BuildServiceProvider();

        var act = () => provider.InitialiseNativeDatabases();

        await act.Should().NotThrowAsync();
    }

    public sealed class TestNativeMigration : NativeSchemaMigration
    {
        public override string Database => DatabaseName;

        public override string Id => "0001_test";

        public override string Sql => "SELECT 1";
    }

    public sealed class OtherDatabaseMigration : NativeSchemaMigration
    {
        public override string Database => "Other";

        public override string Id => "0001_other";

        public override string Sql => "SELECT 1";
    }
}