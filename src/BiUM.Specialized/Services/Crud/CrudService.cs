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

    public CrudService(IServiceProvider serviceProvider, IDbContext dbContext, IConfiguration configuration)
        : base(serviceProvider, dbContext)
    {
        _configuration = configuration;
        _httpClientsService = serviceProvider.GetRequiredService<IHttpClientsService>();
    }
}
