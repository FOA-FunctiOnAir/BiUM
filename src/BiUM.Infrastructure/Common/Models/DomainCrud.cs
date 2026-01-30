using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BiUM.Infrastructure.Common.Models;

[Table("__CRUD", Schema = "dbo")]
public class DomainCrud : TenantBaseEntity
{
    [Column("APPLICATION_ID")]
    public Guid ApplicationId { get; set; }

    [Column("MICROSERVICE_ID")]
    public Guid MicroserviceId { get; set; }

    [Column("NAME")]
    public required string Name { get; set; }

    [Column("CODE")]
    public required string Code { get; set; }

    [Column("TABLE_NAME")]
    public required string TableName { get; set; }

    [JsonIgnore]
    public ICollection<DomainCrudColumn> DomainCrudColumns { get; } = [];

    [JsonIgnore]
    public ICollection<DomainCrudTranslation> DomainCrudTranslations { get; } = [];
}