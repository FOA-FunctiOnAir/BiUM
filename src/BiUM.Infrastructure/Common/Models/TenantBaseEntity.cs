using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BiUM.Infrastructure.Common.Models;

public class TenantBaseEntity : BaseEntity, ITenantBaseEntity
{
    [Required]
    [Column("ID", Order = 0)]
    public override Guid Id { get; set; }

    [Required]
    [Column("CORRELATION_ID", Order = 1)]
    public override Guid CorrelationId { get; set; }

    [Column("TENANT_ID", Order = 2)]
    public Guid TenantId { get; set; }
}