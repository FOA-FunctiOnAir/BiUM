using System;

namespace BiUM.Specialized.Common.DynamicApi;

public enum DynamicApiTableSource
{
    Crud = 1,
    Catalog = 2
}

public class DynamicApiSelectableTableDto
{
    public required string Schema { get; set; }

    public required string TableName { get; set; }

    public DynamicApiTableSource Source { get; set; }

    public string? DisplayName { get; set; }

    public string? CrudCode { get; set; }

    public Guid? CrudId { get; set; }

    public Guid? ApplicationId { get; set; }

    public Guid? TenantId { get; set; }

    public bool IsPublished { get; set; }
}

public class DomainDynamicApisByTableDto
{
    public Guid Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid TenantId { get; set; }

    public Guid MicroserviceId { get; set; }

    public Guid? CompileStatusType { get; set; }
}

public class DomainDynamicApiTableDto
{
    public Guid Id { get; set; }

    public Guid DynamicApiId { get; set; }

    public required string Schema { get; set; }

    public required string TableName { get; set; }
}