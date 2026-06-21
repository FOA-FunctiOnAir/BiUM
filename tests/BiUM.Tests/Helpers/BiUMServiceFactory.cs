using AutoMapper;
using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Infrastructure.Common.Services;
using BiUM.Specialized.Common;
using BiUM.Specialized.Database;
using BiUM.Specialized.Interceptors;
using BiUM.Specialized.Mapping;
using BiUM.Specialized.Services;
using BiUM.Specialized.Services.Compensation;
using BiUM.Specialized.Services.Crud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BiUM.Tests.Helpers;

public static class BiUMServiceFactory
{
    public static ServiceProvider BuildInMemory(
        TestCorrelationContextProvider correlationProvider,
        string databaseName,
        Action<IServiceCollection>? configure = null,
        bool registerRealTranslation = false)
    {
        var services = new ServiceCollection();
        AddCore(services, correlationProvider, registerRealTranslation);

        services.AddScoped(sp =>
        {
            var opts = new DbContextOptionsBuilder<TestBiDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            return new TestBiDbContext(sp, opts, sp.GetRequiredService<EntitySaveChangesInterceptor>());
        });

        configure?.Invoke(services);

        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<TestBiDbContext>());
        services.AddScoped<ICrudService, CrudService>();
        services.AddScoped<ICompensationService, CompensationService>();

        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildSqlite(
        TestCorrelationContextProvider correlationProvider,
        string sqliteDatabasePath,
        Action<IServiceCollection>? configure = null,
        bool registerRealTranslation = false)
    {
        var services = new ServiceCollection();
        AddCore(services, correlationProvider, registerRealTranslation);

        services.AddScoped(sp =>
        {
            var opts = new DbContextOptionsBuilder<TestBiDbContext>()
                .UseSqlite($"Data Source={sqliteDatabasePath}")
                .Options;
            return new TestBiDbContext(sp, opts, sp.GetRequiredService<EntitySaveChangesInterceptor>());
        });

        configure?.Invoke(services);

        services.AddScoped<IDbContext>(sp => sp.GetRequiredService<TestBiDbContext>());
        services.AddScoped<ICrudService, CrudService>();
        services.AddScoped<ICompensationService, CompensationService>();

        return services.BuildServiceProvider();
    }

    private static void AddCore(
        ServiceCollection services,
        TestCorrelationContextProvider correlationProvider,
        bool registerRealTranslation)
    {
        services.AddSingleton<ICorrelationContextProvider>(correlationProvider);

        var dateTimeMock = new Mock<IDateTimeService>();
        dateTimeMock.Setup(d => d.Now).Returns(DateTime.UtcNow);
        dateTimeMock.Setup(d => d.OffsetNow).Returns(DateTimeOffset.UtcNow);
        dateTimeMock.Setup(d => d.Today).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetToday).Returns(DateOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.TimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));
        dateTimeMock.Setup(d => d.OffsetTimeNow).Returns(TimeOnly.FromDateTime(DateTime.UtcNow));
        services.AddSingleton(dateTimeMock.Object);

        services.AddSingleton(Options.Create(new BiAppOptions
        {
            Environment = "Test",
            Domain = "BiUM.Tests",
            Port = 0,
            EncryptionKey = string.Empty
        }));

        if (registerRealTranslation)
        {
            services.AddScoped<ITranslationService, TranslationService>();
        }
        else
        {
            var translationMock = new Mock<ITranslationService>();
            services.AddSingleton(translationMock.Object);
        }

        services.AddSingleton<ILogger<SpecializedBase>>(_ => NullLogger<SpecializedBase>.Instance);

        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, NullLoggerFactory.Instance);
        services.AddSingleton<IMapper>(mapperConfig.CreateMapper());

        var httpMock = new Mock<IHttpClientsService>();
        httpMock
            .Setup(h => h.CallService<ApiResponse>(
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, dynamic>?>(),
                It.IsAny<IReadOnlyList<Guid>?>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResponse<ApiResponse>(new ApiResponse()));
        services.AddSingleton(httpMock.Object);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseType"] = "PostgreSQL"
            })
            .Build();
        services.AddSingleton<IConfiguration>(configuration);

        services.AddHttpClient();

        services.AddScoped<EntitySaveChangesInterceptor>();
    }
}