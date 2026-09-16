using BiUM.Contract.Models.Api;
using BiUM.Core.Authorization;
using BiUM.Infrastructure.Common.Models;
using BiUM.Specialized.Common.Crud;
using BiUM.Specialized.Database;
using BiUM.Specialized.Services.Crud;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BiApp.Test.Infrastructure.Crud;

public class SampleCrudService : CrudService
{
    private readonly ICorrelationContextAccessor _accessor;
    private readonly ICorrelationContextProvider _provider;

    public SampleCrudService(IServiceProvider serviceProvider, IDbContext dbContext, IConfiguration configuration)
        : base(serviceProvider, dbContext, configuration)
    {
        _accessor = serviceProvider.GetRequiredService<ICorrelationContextAccessor>();
        _provider = serviceProvider.GetRequiredService<ICorrelationContextProvider>();
    }

    public override async Task<ApiResponse> SaveAsync(string code, Dictionary<string, object?> data, CancellationToken cancellationToken)
    {
        SampleCrudContextHelper.Ensure(_accessor, _provider);
        return await base.SaveAsync(code, data, cancellationToken);
    }

    public override async Task<ApiResponse> SaveDomainCrudAsync(
        SaveDomainCrudCommand command,
        CancellationToken cancellationToken)
    {
        var response = await base.SaveDomainCrudAsync(command, cancellationToken);

        if (!response.Success || command.Id != SampleCrudConstants.NotesCrudId)
        {
            return response;
        }

        var crud = await DbContext.DomainCruds
            .FirstOrDefaultAsync(x => x.Id == SampleCrudConstants.NotesCrudId, cancellationToken);

        if (crud is null || crud.TenantId != Guid.Empty)
        {
            return response;
        }

        var tenantId = CorrelationContext.TenantId ?? SampleCrudConstants.TenantId;

        if (tenantId == Guid.Empty)
        {
            return response;
        }

        crud.TenantId = tenantId;
        _ = await DbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    protected override Task<ApiResponse> SaveCrudServicesAsync(
        Guid applicationId,
        Guid microserviceId,
        string code,
        ICollection<DomainCrudVersionColumn> columns,
        IReadOnlyList<SaveCrudServicesPartialPayloadDto> partials,
        CancellationToken cancellationToken) =>
        Task.FromResult(new ApiResponse());
}