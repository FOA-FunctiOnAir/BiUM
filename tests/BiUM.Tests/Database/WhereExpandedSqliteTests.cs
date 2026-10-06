using BiApp.Test.Domain.Entities;
using BiApp.Test.Infrastructure.Persistence;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Services;
using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BiUM.Tests.Database;

public sealed class WhereExpandedSqliteTests : IDisposable
{
    private static readonly Guid LanguageTr = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000001");
    private static readonly Guid LanguageEn = Guid.Parse("aaaaaaaa-1111-0000-0000-000000000002");

    private static readonly Guid NilayLowerId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
    private static readonly Guid NilayUpperAsciiId = Guid.Parse("dddddddd-0000-0000-0000-000000000002");
    private static readonly Guid NilayUpperTurkishId = Guid.Parse("dddddddd-0000-0000-0000-000000000003");

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _sp;

    public WhereExpandedSqliteTests()
    {
        _connection = new SqliteConnection("Data Source=WhereExpandedTest;Mode=Memory;Cache=Shared");
        _connection.Open();

        var correlationProvider = new TestCorrelationContextProvider();

        var dateTimeMock = new Mock<IDateTimeService>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);
        dateTimeMock.Setup(d => d.OffsetNow).Returns(DateTimeOffset.UtcNow);
        dateTimeMock.Setup(d => d.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetToday).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.TimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetTimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));

        var services = new ServiceCollection();
        services.AddSingleton<ICorrelationContextProvider>(correlationProvider);
        var correlationContextAccessor = TestCorrelationContextBootstrap.RegisterSharedAccessor();
        correlationContextAccessor.CorrelationContext = correlationProvider.Context;
        services.AddSingleton<ICorrelationContextAccessor>(correlationContextAccessor);
        services.AddSingleton(dateTimeMock.Object);
        services.AddSingleton(Options.Create(new BiAppOptions
        {
            Environment = "Test",
            Domain = "BiUM.Tests",
            Port = 0,
            EncryptionKey = string.Empty
        }));
        services.AddScoped<EntitySaveChangesInterceptor>();
        services.AddScoped(sp =>
        {
            var opts = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;
            return new TestDbContext(sp, opts, sp.GetRequiredService<EntitySaveChangesInterceptor>());
        });

        _sp = services.BuildServiceProvider();

        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        ctx.Database.EnsureCreated();
        SeedData(ctx);
        ctx.Database.CurrentTransaction?.Commit();

        var count = ctx.Currencies.Count();
        if (count == 0) throw new InvalidOperationException("Seed failed: no currencies found after SaveChanges.");
    }

    private static void SeedData(TestDbContext ctx)
    {
        ctx.Currencies.AddRange(
            new Currency { Id = NilayLowerId, Name = "Nilay", Code = "NLY" },
            new Currency { Id = NilayUpperAsciiId, Name = "NILAY", Code = "NIA" },
            new Currency { Id = NilayUpperTurkishId, Name = "NİLAY", Code = "NIT" });

        ctx.CurrencyTranslations.AddRange(
            new CurrencyTranslation { RecordId = NilayLowerId, LanguageId = LanguageTr, Column = nameof(Currency.Name), Translation = "Nilay (TR)" },
            new CurrencyTranslation { RecordId = NilayLowerId, LanguageId = LanguageEn, Column = nameof(Currency.Name), Translation = "Nilay (EN)" });

        ctx.SaveChanges();
    }

    [Theory]
    // stored "Nilay": ı not expected to match, I/İ expected to match
    [InlineData("Nilay", "Nıl", false)]
    [InlineData("Nilay", "NIL", true)]
    [InlineData("Nilay", "NİL", true)]
    // stored "NILAY": İ not expected to match, ı/i expected to match
    [InlineData("NILAY", "NİL", false)]
    [InlineData("NILAY", "nıl", true)]
    [InlineData("NILAY", "nil", true)]
    // stored "NİLAY": ı/I not expected to match, İ/i expected to match
    [InlineData("NİLAY", "nıl", false)]
    [InlineData("NİLAY", "NIL", false)]
    [InlineData("NİLAY", "NİL", true)]
    [InlineData("NİLAY", "nil", true)]
    public async Task WhereExpanded_ApplyContains_matches_Turkish_and_invariant_folding_correctly(
        string storedName, string search, bool shouldMatch)
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var match = await ctx.Currencies
            .WhereExpanded(c => c.Name == storedName && c.ApplyContains(x => x.Name, search))
            .AnyAsync();

        match.Should().Be(shouldMatch);
    }

    [Fact]
    public async Task WhereExpanded_combines_ApplyContains_with_plain_conditions_via_and()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var resultsWithWrongCode = await ctx.Currencies
            .WhereExpanded(c => c.Code == "DOES_NOT_EXIST" && c.ApplyContains(x => x.Name, "nil"))
            .ToListAsync();

        var resultsWithRightCode = await ctx.Currencies
            .WhereExpanded(c => c.Code == "NLY" && c.ApplyContains(x => x.Name, "nil"))
            .ToListAsync();

        resultsWithWrongCode.Should().BeEmpty();
        resultsWithRightCode.Should().ContainSingle(c => c.Id == NilayLowerId);
    }

    [Fact]
    public async Task WhereExpanded_combines_ApplyContains_with_or()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var results = await ctx.Currencies
            .WhereExpanded(c => c.Code == "DOES_NOT_EXIST" || c.ApplyContains(x => x.Code, "nia"))
            .ToListAsync();

        results.Should().ContainSingle(c => c.Id == NilayUpperAsciiId);
    }

    [Fact]
    public async Task WhereExpanded_ApplyAnyContains_matches_translation_table_for_requested_language_only()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var matchesTr = await ctx.Currencies
            .WhereExpanded(c => c.ApplyAnyContains(
                x => x.CurrencyTranslations,
                t => t.Translation,
                "nilay (tr)",
                childFilter: t => t.LanguageId == LanguageTr))
            .ToListAsync();

        var matchesEnForTrSearch = await ctx.Currencies
            .WhereExpanded(c => c.ApplyAnyContains(
                x => x.CurrencyTranslations,
                t => t.Translation,
                "nilay (tr)",
                childFilter: t => t.LanguageId == LanguageEn))
            .ToListAsync();

        matchesTr.Should().ContainSingle(c => c.Id == NilayLowerId);
        matchesEnForTrSearch.Should().BeEmpty();
    }

    [Fact]
    public async Task WhereExpanded_with_empty_search_behaves_as_no_filter()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var all = await ctx.Currencies.CountAsync();

        var result = await ctx.Currencies
            .WhereExpanded(c => c.ApplyContains(x => x.Name, string.Empty))
            .CountAsync();

        result.Should().Be(all);
    }

    public void Dispose()
    {
        _sp.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}