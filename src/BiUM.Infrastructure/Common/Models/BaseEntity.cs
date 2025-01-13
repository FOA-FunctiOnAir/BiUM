using BiUM.Infrastructure.Common.Events;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiUM.Infrastructure.Common.Models;

public class BaseEntity : BaseAuditableEntity, IBaseEntity
{
    [Required]
    [Column("ID", Order = 1)]
    public override Guid Id { get; set; }

    [Required]
    [Column("ACTIVE", Order = 2)]
    public bool Active { get; set; } = true;

    [Required]
    [Column("DELETED", Order = 3)]
    public bool Deleted { get; set; } = false;

    [Column("CREATED", Order = 4)]
    public DateOnly Created { get; set; }

    [Column("CREATED_TIME", Order = 5)]
    public TimeOnly CreatedTime { get; set; }

    [Column("CREATED_BY", Order = 6)]
    public Guid? CreatedBy { get; set; }

    [Column("UPDATED", Order = 7)]
    public DateOnly? Updated { get; set; }

    [Column("UPDATED_TIME", Order = 8)]
    public TimeOnly? UpdatedTime { get; set; }

    [Column("UPDATED_BY", Order = 8)]
    public Guid? UpdatedBy { get; set; }

    [Required]
    [Column("TEST", Order = 10)]
    public bool Test { get; set; } = false;

    public BaseEntity()
    {
        Id = Guid.NewGuid();
        _domainEvents = [];
    }

    [NotMapped]
    private IList<IBaseEvent> _domainEvents { get; set; }

    [NotMapped]
    public IList<IBaseEvent> DomainEvents { get { return _domainEvents; } }

    public void AddDomainEvent(BaseEvent baseEvent)
    {
        _domainEvents.Add(baseEvent);
    }
}