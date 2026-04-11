using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_PARTIAL_UPDATE_COLUMN", Schema = "dbo")]
public class DomainCrudPartialUpdateColumn : BaseEntity
{
    [Column("PARTIAL_UPDATE_ID")]
    public Guid PartialUpdateId { get; set; }

    [Column("CRUD_COLUMN_ID")]
    public Guid CrudColumnId { get; set; }

    [ForeignKey(nameof(PartialUpdateId))]
    [JsonIgnore]
    public DomainCrudPartialUpdate PartialUpdate { get; private set; } = null!;

    [ForeignKey(nameof(CrudColumnId))]
    [JsonIgnore]
    public DomainCrudColumn CrudColumn { get; private set; } = null!;
}