using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_VERSION_PARTIAL_UPDATE_COLUMN", Schema = "dbo")]
public class DomainCrudVersionPartialUpdateColumn : BaseEntity
{
    [Column("VERSION_PARTIAL_UPDATE_ID")]
    public Guid VersionPartialUpdateId { get; set; }

    [Column("CRUD_VERSION_COLUMN_ID")]
    public Guid CrudVersionColumnId { get; set; }

    [ForeignKey(nameof(VersionPartialUpdateId))]
    [JsonIgnore]
    public DomainCrudVersionPartialUpdate VersionPartialUpdate { get; private set; } = null!;

    [ForeignKey(nameof(CrudVersionColumnId))]
    [JsonIgnore]
    public DomainCrudVersionColumn CrudVersionColumn { get; private set; } = null!;
}