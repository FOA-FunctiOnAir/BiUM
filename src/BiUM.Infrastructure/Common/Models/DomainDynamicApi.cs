using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API", Schema = "dbo")]
public class DomainDynamicApi : TenantBaseEntity
{
    [Column("CODE")]
    public required string Code { get; set; }

    [Column("NAME")]
    public required string Name { get; set; }

    [Column("MICROSERVICE_ID")]
    public required Guid MicroserviceId { get; set; }

    [Column("HTTP_TYPE")]
    public required Guid HttpType { get; set; }

    [Column("EXECUTION_TYPE")]
    public required Guid ExecutionType { get; set; }

    [Column("RUNTIME_PLATFORM_TYPE")]
    public required Guid RuntimePlatformType { get; set; }

    [Column("SOURCE_CODE")]
    public required string SourceCode { get; set; }

    [Column("COMPILE_STATUS_TYPE")]
    public Guid? CompileStatusType { get; set; }

    [Column("COMPILE_ERROR")]
    public string? CompileError { get; set; }

    public virtual ICollection<DomainDynamicApiParameter>? Parameters { get; set; }
}