using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_EXPORT_REQUEST", Schema = "dbo")]
public class DomainDynamicExportRequest : TenantBaseEntity
{
    [Column("APPLICATION_ID")]
    public Guid ApplicationId { get; set; }

    [Column("NAME")]
    public required string Name { get; set; }

    [Column("STATUS")]
    public Guid Status { get; set; }

    [Column("SOURCE_URL")]
    public required string SourceUrl { get; set; }

    [Column("SOURCE_MICROSERVICE_ID")]
    public Guid? SourceMicroserviceId { get; set; }

    [Column("SOURCE_PARAMETERS")]
    public string? SourceParameters { get; set; }

    [Column("FORMAT")]
    public required string Format { get; set; }

    [Column("FILE_NAME")]
    public string? FileName { get; set; }

    [Column("MIME_TYPE")]
    public string? MimeType { get; set; }

    [Column("ROW_COUNT")]
    public int? RowCount { get; set; }

    [Column("SOURCE_TOTAL_COUNT")]
    public int? SourceTotalCount { get; set; }

    [Column("TRUNCATED")]
    public bool Truncated { get; set; }

    [Column("ERROR_MESSAGE")]
    public string? ErrorMessage { get; set; }

    [Column("EXPIRES_AT")]
    public DateTime? ExpiresAt { get; set; }

    [JsonIgnore]
    public DomainDynamicExportFile? ExportFile { get; private set; }
}