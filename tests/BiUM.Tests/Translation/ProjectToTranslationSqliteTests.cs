using AutoMapper;
using BiApp.Test.Application.Features.Currencies.Queries.GetFwCurrenciesForNames;
using BiApp.Test.Domain.Entities;
using BiApp.Test.Infrastructure.Persistence;
using BiUM.Contract.Models;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Common.Services;
using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Mapping;
using BiUM.Tests.Helpers;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BiUM.Tests.Translation;

public sealed class ProjectToTranslationSqliteTests : IDisposable
{
    private static readonly Guid LanguageTr = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid LanguageEn = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid CurrencyId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _sp;
    private readonly IMapper _mapper;

    public ProjectToTranslationSqliteTests()
    {
        _connection = new SqliteConnection("Data Source=ProjectToTranslationTest;Mode=Memory;Cache=Shared");
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

        var mapperConfig = new MapperConfiguration(
            cfg => cfg.AddProfile(new MappingProfile(typeof(GetFwCurrenciesForNamesDto).Assembly)),
            NullLoggerFactory.Instance);
        _mapper = mapperConfig.CreateMapper();

        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        ctx.Database.EnsureCreated();
        SeedData(ctx);
        ctx.Database.CurrentTransaction?.Commit();
        var count = ctx.Currencies.Count();
        if (count == 0) throw new InvalidOperationException($"Seed failed: no currencies found after SaveChanges. Connection state: {_connection.State}");
    }

    private static void SeedData(TestDbContext ctx)
    {
        ctx.Currencies.Add(new Currency
        {
            Id = CurrencyId,
            Name = "US Dollar",
            Code = "USD"
        });

        ctx.CurrencyTranslations.AddRange(
            new CurrencyTranslation
            {
                RecordId = CurrencyId,
                LanguageId = LanguageTr,
                Column = nameof(Currency.Name),
                Translation = "Amerikan Doları"
            },
            new CurrencyTranslation
            {
                RecordId = CurrencyId,
                LanguageId = LanguageEn,
                Column = nameof(Currency.Name),
                Translation = "US Dollar"
            });

        ctx.SaveChanges();
    }

    [Fact]
    public async Task ToListAsync_with_languageId_returns_correct_translation()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .ToListAsync<Currency, GetFwCurrenciesForNamesDto>(_mapper, LanguageTr, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Amerikan Doları");
    }

    [Fact]
    public async Task ToListAsync_switches_to_different_language()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .ToListAsync<Currency, GetFwCurrenciesForNamesDto>(_mapper, LanguageEn, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("US Dollar");
    }

    [Fact]
    public async Task ToListAsync_without_explicit_languageId_uses_correlation_context_language()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICorrelationContextAccessor>();
        accessor.CorrelationContext = new CorrelationContext
        {
            CorrelationId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid(),
            LanguageId = LanguageTr
        };

        var result = await ctx.Currencies
            .ToListAsync<Currency, GetFwCurrenciesForNamesDto>(_mapper, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Amerikan Doları");
    }

    [Fact]
    public async Task FirstOrDefaultAsync_with_languageId_returns_correct_translation()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .FirstOrDefaultAsync<Currency, GetFwCurrenciesForNamesDto>(
                c => c.Id == CurrencyId,
                _mapper,
                LanguageTr,
                CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Amerikan Doları");
    }

    [Fact]
    public async Task ToPaginatedListAsync_with_languageId_returns_correct_translation()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .ToPaginatedListAsync<Currency, GetFwCurrenciesForNamesDto>(
                PaginationQuery.ToPageBaseQuery(0, 10, "Id"),
                _mapper,
                LanguageTr,
                CancellationToken.None);

        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Amerikan Doları");
    }

    [Fact]
    public async Task ToPaginatedListAsync_without_sortBy_orders_on_source_entity()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .ToPaginatedListAsync<Currency, GetFwCurrenciesForNamesDto>(
                PaginationQuery.ToPageBaseQuery(0, 10),
                _mapper,
                LanguageTr,
                CancellationToken.None);

        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Amerikan Doları");
    }

    [Fact]
    public async Task WhereToListAsync_with_languageId_returns_correct_translation()
    {
        using var scope = _sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var result = await ctx.Currencies
            .WhereToListAsync<Currency, GetFwCurrenciesForNamesDto>(
                c => c.Active,
                _mapper,
                LanguageTr,
                CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Amerikan Doları");
    }

    public void Dispose()
    {
        _sp.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}