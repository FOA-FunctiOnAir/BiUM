using BiUM.Specialized.Common.MediatR;
using System;

namespace BiUM.Specialized.Common.DynamicApi;

public record GetDomainDynamicApisQuery : BasePaginatedQueryDto<DomainDynamicApisDto>
{
    public Guid? ApplicationId { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
}

public record GetDynamicApiSelectableTablesQuery
{
    public Guid ApplicationId { get; set; }

    public Guid MicroserviceId { get; set; }
}

public record GetDomainDynamicApisByTableQuery
{
    public required string Schema { get; set; }

    public required string TableName { get; set; }
}