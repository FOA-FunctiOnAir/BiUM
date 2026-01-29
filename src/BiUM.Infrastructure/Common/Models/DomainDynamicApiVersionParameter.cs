using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_VERSION_PARAMETER", Schema = "dbo")]
public class DomainDynamicApiVersionParameter : BaseEntity
{
    [Column("DYNAMIC_API_VERSION_ID")]
    public Guid DynamicApiVersionId { get; set; }

    [Column("DIRECTION_TYPE")]
    public Guid DirectionType { get; set; }

    [Column("PROPERTY")]
    public required string Property { get; set; }

    [Column("FIELD_ID")]
    public Guid FieldId { get; set; }

    [ForeignKey(nameof(DynamicApiVersionId))]
    [JsonIgnore]
    public DomainDynamicApiVersion DynamicApiVersion { get; private set; } = null!;
}
