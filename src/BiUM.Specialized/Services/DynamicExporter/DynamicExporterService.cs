using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Specialized.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace BiUM.Specialized.Services.DynamicExporter;

public partial class DynamicExporterService : BaseRepository, IDynamicExporterService
{
    private readonly IHttpClientsService _httpClientsService;
    private readonly DynamicExporterOptions _options;

    public DynamicExporterService(
        IServiceProvider serviceProvider,
        IDbContext dbContext,
        IOptions<DynamicExporterOptions> options)
        : base(serviceProvider, dbContext)
    {
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
        _options = options.Value;
    }
}