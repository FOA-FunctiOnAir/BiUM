using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_VERSION", Schema = "dbo")]
public class DomainDynamicApiVersion : BaseEntity
{
    [Column("DYNAMIC_API_ID")]
    public Guid DynamicApiId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("VERSION")]
    public int Version { get; set; }

    [Column("HTTP_TYPE")]
    public Guid HttpType { get; set; }

    [Column("EXECUTION_TYPE")]
    public Guid ExecutionType { get; set; }

    [Column("RUNTIME_PLATFORM_TYPE")]
    public Guid RuntimePlatformType { get; set; }

    [Column("SOURCE_CODE")]
    public required string SourceCode { get; set; }

    [ForeignKey(nameof(DynamicApiId))]
    [JsonIgnore]
    public DomainDynamicApi DynamicApi { get; private set; } = null!;

    [ForeignKey(nameof(DomainDynamicApiVersionParameter.DynamicApiVersionId))]
    [JsonIgnore]
    public ICollection<DomainDynamicApiVersionParameter> DynamicApiVersionParameters { get; } = [];
}
