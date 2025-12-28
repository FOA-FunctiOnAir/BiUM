using AutoMapper;
using BiUM.Core.Authorization;
using BiUM.Core.Common.Configs;
using BiUM.Core.HttpClients;
using BiUM.Core.Models;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService : BaseRepository, ICrudService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IDbContext _baseContext;

    private readonly ICorrelationContextProvider _correlationContextProvider;
    private readonly CorrelationContext _correlationContext;
    private readonly IHttpClientsService _httpClientsService;
    private readonly IMapper _mapper;

    private readonly BiAppOptions _biAppOptions;

    public CrudService(IServiceProvider serviceProvider, IDbContext baseContext, IConfiguration configuration)
        : base(serviceProvider, baseContext)
    {
        _serviceProvider = serviceProvider;
        _baseContext = baseContext;
        _configuration = configuration;

        _correlationContextProvider = _serviceProvider.GetRequiredService<ICorrelationContextProvider>();
        _httpClientsService = _serviceProvider.GetRequiredService<IHttpClientsService>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();

        _biAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;

        _correlationContext = _correlationContextProvider.Get() ?? CorrelationContext.Empty;
    }
}
