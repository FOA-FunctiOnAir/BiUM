using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_PARTIAL_UPDATE", Schema = "dbo")]
public class DomainCrudPartialUpdate : TenantBaseEntity
{
    [Column("CRUD_ID")]
    public Guid CrudId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("NAME")]
    public string? Name { get; set; }

    [ForeignKey(nameof(CrudId))]
    [JsonIgnore]
    public DomainCrud Crud { get; private set; } = null!;

    [JsonIgnore]
    public ICollection<DomainCrudPartialUpdateColumn> Columns { get; } = [];
}