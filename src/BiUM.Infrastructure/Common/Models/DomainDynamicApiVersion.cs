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

    [Column("COMPILED_ASSEMBLY")]
    public byte[]? CompiledAssembly { get; set; }

    [Column("ASSEMBLY_HASH")]
    public string? AssemblyHash { get; set; }

    [Column("ENTRY_POINT_TYPE_NAME")]
    public string? EntryPointTypeName { get; set; }

    [ForeignKey(nameof(DynamicApiId))]
    [JsonIgnore]
    public DomainDynamicApi DynamicApi { get; private set; } = null!;

    [JsonIgnore]
    public ICollection<DomainDynamicApiVersionParameter> DynamicApiVersionParameters { get; } = [];
}