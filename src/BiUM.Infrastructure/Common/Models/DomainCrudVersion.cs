using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_VERSION", Schema = "dbo")]
public class DomainCrudVersion : TenantBaseEntity
{
    [Column("APPLICATION_ID")]
    public Guid ApplicationId { get; set; }

    [Column("CRUD_ID")]
    public Guid CrudId { get; set; }

    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [Column("VERSION")]
    public int Version { get; set; }

    [ForeignKey(nameof(CrudId))]
    [JsonIgnore]
    public DomainCrud DomainCrud { get; private set; } = null!;

    [JsonIgnore]
    public ICollection<DomainCrudVersionColumn> DomainCrudVersionColumns { get; } = [];
}