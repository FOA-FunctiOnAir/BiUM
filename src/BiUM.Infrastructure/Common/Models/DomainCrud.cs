using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD", Schema = "dbo")]
public class DomainCrud : TenantBaseEntity
{
    [Column("MICROSERVICE_ID")]
    public Guid MicroserviceId { get; set; }

    [Column("NAME")]
    public required string Name { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("TABLE_NAME")]
    public required string TableName { get; set; }


    public virtual ICollection<DomainCrudColumn>? DomainCrudColumns { get; set; }

    public virtual ICollection<DomainCrudTranslation>? DomainCrudTranslations { get; set; }
}
