using BiUM.Core.HttpClients;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BiUM.Specialized.Services.DynamicApi;

public partial class DynamicApiService : BaseRepository, IDynamicApiService
{
    private readonly IHttpClientsService _httpClientsService;
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
        _runtimeCache = runtimeCache;
        _dbType = configuration.GetValue<string>("DatabaseType") ?? DynamicApiSchemaRules.DbTypePostgresql;
    }
}