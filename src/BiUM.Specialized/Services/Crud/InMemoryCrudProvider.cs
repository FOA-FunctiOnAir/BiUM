//using AutoMapper;
//using BiUM.Infrastructure.Common.Models;
//using BiUM.Infrastructure.Services.Authorization;
//using BiUM.Specialized.Common.API;
//using BiUM.Specialized.Common.Crud;
//using BiUM.Specialized.Database;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;

//namespace BiUM.Specialized.Services.Crud;

//public sealed class InMemoryCrudProvider : ICrudProvider
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly IDbContext _baseContext;

//    public readonly ICorrelationContextProvider _correlationContextProvider;
//    public readonly IMapper _mapper;

//    private readonly List<DomainCrud> _defs;

//    public InMemoryCrudProvider(IServiceProvider serviceProvider)
//    {
//        _serviceProvider = serviceProvider;

//        _baseContext = _serviceProvider.GetRequiredService<IDbContext>();

//        _correlationContextProvider = _serviceProvider.GetRequiredService<ICorrelationContextProvider>();
//        _mapper = _serviceProvider.GetRequiredService<IMapper>();
//    }

//    public IEnumerable<DomainCrud> GetPublishedCruds() => _defs;

//    public DomainCrud Get(string code) =>
//        _defs.First(d => string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase));

//    private async Task<ApiResponse<DomainCrudDto>> GetDomainCrudByCodeAsync(
//        string code,
//        CancellationToken cancellationToken)
//    {
//        var returnObject = new ApiResponse<DomainCrudDto>();

//        var domainCrud = await _baseContext.DomainCruds
//            .Include(p => p.DomainCrudTranslations)
//            .Include(m => m.DomainCrudColumns)
//            .FirstOrDefaultAsync<DomainCrud, DomainCrudDto>(x => x.Code == code, _mapper, cancellationToken);

//        returnObject.Value = domainCrud;

//        return returnObject;
//    }
//}