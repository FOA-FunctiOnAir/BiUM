using AutoMapper;
using BiUM.Core.Common.Configs;
using BiUM.Infrastructure.Services.Authorization;
using BiUM.Specialized.Database;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BiUM.Specialized.Services.Crud;

public partial class CrudService : BaseRepository, ICrudService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IDbContext _baseContext;

    public readonly ICurrentUserService _currentUserService;
    public readonly IMapper _mapper;

    public readonly BiAppOptions _biAppOptions;

    public CrudService(IServiceProvider serviceProvider, IDbContext baseContext, IConfiguration configuration)
        : base(serviceProvider, baseContext)
    {
        _serviceProvider = serviceProvider;
        _baseContext = baseContext;
        _configuration = configuration;

        _currentUserService = _serviceProvider.GetRequiredService<ICurrentUserService>();
        _mapper = _serviceProvider.GetRequiredService<IMapper>();

        _biAppOptions = _serviceProvider.GetRequiredService<IOptions<BiAppOptions>>().Value;
    }
}