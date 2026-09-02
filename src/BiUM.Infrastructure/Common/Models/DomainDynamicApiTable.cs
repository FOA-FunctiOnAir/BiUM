using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__DYNAMIC_API_TABLE", Schema = "dbo")]
public class DomainDynamicApiTable : BaseEntity
{
    [Column("DYNAMIC_API_ID")]
    public Guid DynamicApiId { get; set; }

    [Column("SCHEMA")]
    public required string Schema { get; set; }

    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [ForeignKey(nameof(DynamicApiId))]
    [JsonIgnore]
    public DomainDynamicApi DynamicApi { get; private set; } = null!;
}