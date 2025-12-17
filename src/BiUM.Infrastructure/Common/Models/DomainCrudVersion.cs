using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD_VERSION", Schema = "dbo")]
public class DomainCrudVersion : TenantBaseEntity
{
    [Column("CRUD_ID")]
    public Guid CrudId { get; set; }

    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [Column("VERSION")]
    public required int Version { get; set; }


    [ForeignKey("CrudId")]
    public virtual DomainCrud? DomainCrud { get; set; }

    public virtual IList<DomainCrudVersionColumn>? DomainCrudVersionColumns { get; set; }
}