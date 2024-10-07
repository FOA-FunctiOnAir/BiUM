using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BiUM.Infrastructure.Common.Models;

public class TenantBaseEntity : BaseEntity, ITenantBaseEntity
{
    [Required]
    [Column("ID", Order = 0)]
    public override Guid Id { get; set; }

    [Column("TENANT_ID", Order = 1)]
    public Guid TenantId { get; set; }
}