using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_PARAMETER", Schema = "dbo")]
public class DomainDynamicApiParameter : BaseEntity
{
    [Column("DYNAMIC_API_ID")]
    public Guid DynamicApiId { get; set; }

    [Column("DIRECTION_TYPE")]
    public required Guid DirectionType { get; set; }

    [Column("PROPERTY")]
    public required string Property { get; set; }

    [Column("FIELD_ID")]
    public required Guid FieldId { get; set; }

    [ForeignKey(nameof(DynamicApiId))]
    public virtual DomainDynamicApi? DynamicApi { get; set; }
}