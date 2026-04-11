using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_VERSION_PARTIAL_UPDATE", Schema = "dbo")]
public class DomainCrudVersionPartialUpdate : TenantBaseEntity
{
    [Column("CRUD_VERSION_ID")]
    public Guid CrudVersionId { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("NAME")]
    public string? Name { get; set; }

    [ForeignKey(nameof(CrudVersionId))]
    [JsonIgnore]
    public DomainCrudVersion CrudVersion { get; private set; } = null!;

    [JsonIgnore]
    public ICollection<DomainCrudVersionPartialUpdateColumn> Columns { get; } = [];
}