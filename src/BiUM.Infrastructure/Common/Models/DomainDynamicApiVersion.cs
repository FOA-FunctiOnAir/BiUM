using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_VERSION", Schema = "dbo")]
public class DomainDynamicApiVersion : BaseEntity
{
    [Column("DYNAMIC_API_ID")]
    public required Guid DynamicApiId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("VERSION")]
    public required int Version { get; set; }

    [Column("HTTP_TYPE")]
    public required Guid HttpType { get; set; }

    [Column("EXECUTION_TYPE")]
    public required Guid ExecutionType { get; set; }

    [Column("RUNTIME_PLATFORM_TYPE")]
    public required Guid RuntimePlatformType { get; set; }

    [Column("SOURCE_CODE")]
    public required string SourceCode { get; set; }

    public virtual ICollection<DomainDynamicApiVersionParameter>? Parameters { get; set; }

    [ForeignKey(nameof(DynamicApiId))]
    public virtual DomainDynamicApi? DynamicApi { get; set; }
}