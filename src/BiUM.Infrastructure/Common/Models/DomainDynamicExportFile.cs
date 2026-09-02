using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_EXPORT_FILE", Schema = "dbo")]
public class DomainDynamicExportFile : BaseEntity
{
    [Column("EXPORT_REQUEST_ID")]
    public Guid ExportRequestId { get; set; }

    [Column("CONTENT")]
    public byte[]? Content { get; set; }

    [Column("STORAGE_PATH")]
    public string? StoragePath { get; set; }

    [ForeignKey(nameof(ExportRequestId))]
    [JsonIgnore]
    public DomainDynamicExportRequest ExportRequest { get; private set; } = null!;
}