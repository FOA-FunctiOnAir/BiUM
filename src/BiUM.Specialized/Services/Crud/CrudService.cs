using BiUM.Core.Caching.InMemory;
using BiUM.Core.Caching.Redis;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService : BaseRepository, ICrudService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientsService _httpClientsService;
    private readonly IRedisClient? _redisClient;
    private readonly IInMemoryClient? _inMemoryClient;
    private readonly string _dbType;

    public CrudService(IServiceProvider serviceProvider, IDbContext dbContext, IConfiguration configuration)
        : base(serviceProvider, dbContext)
    {
        _configuration = configuration;
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
        _redisClient = serviceProvider.GetService<IRedisClient>();
        _inMemoryClient = serviceProvider.GetService<IInMemoryClient>();
        _dbType = configuration.GetValue<string>("DatabaseType") ?? DbTypePostgresql;
    }
}