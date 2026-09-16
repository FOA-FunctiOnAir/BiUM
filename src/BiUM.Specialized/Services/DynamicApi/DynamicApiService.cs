using BiUM.Core.Caching.InMemory;
using BiUM.Core.Caching.Redis;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService : BaseRepository, IDynamicApiService
{
    private readonly IHttpClientsService _httpClientsService;
    private readonly IRedisClient? _redisClient;
    private readonly IInMemoryClient? _inMemoryClient;
    private readonly IMemoryCache _memoryCache;
    private readonly IDynamicApiEventPublisher _eventPublisher;
    private readonly DynamicApiRuntimeCache _runtimeCache;
    private readonly string _dbType;

    public DynamicApiService(
        IServiceProvider serviceProvider,
        IDbContext dbContext,
        DynamicApiRuntimeCache runtimeCache,
        IConfiguration configuration)
        : base(serviceProvider, dbContext)
    {
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
        _redisClient = serviceProvider.GetService<IRedisClient>();
        _inMemoryClient = serviceProvider.GetService<IInMemoryClient>();
        _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        _eventPublisher = serviceProvider.GetService<IDynamicApiEventPublisher>() ?? new NoOpDynamicApiEventPublisher();
        _runtimeCache = runtimeCache;
        _dbType = configuration.GetValue<string>("DatabaseType") ?? DynamicApiSchemaRules.DbTypePostgresql;
    }
}