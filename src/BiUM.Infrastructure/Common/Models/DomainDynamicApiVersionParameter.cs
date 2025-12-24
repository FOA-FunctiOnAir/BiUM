using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_VERSION_PARAMETER", Schema = "dbo")]
public class DomainDynamicApiVersionParameter : BaseEntity
{
    [Column("DYNAMIC_API_VERSION_ID")]
    public Guid DynamicApiVersionId { get; set; }

    [Column("DIRECTION_TYPE")]
    public required Guid DirectionType { get; set; }

    [Column("PROPERTY")]
    public required string Property { get; set; }

    [Column("FIELD_ID")]
    public required Guid FieldId { get; set; }

    [ForeignKey(nameof(DynamicApiVersionId))]
    public virtual DomainDynamicApiVersion? DynamicApiVersion { get; set; }
}