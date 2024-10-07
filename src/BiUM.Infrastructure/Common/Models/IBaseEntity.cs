using BiUM.Infrastructure.Common.Events;

namespace BiUM.Infrastructure.Common.Models;

public interface IBaseEntity
{
    public Guid Id { get; set; }

    public bool Active { get; set; }

    public DateOnly Created { get; set; }

    public TimeOnly CreatedTime { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateOnly? Updated { get; set; }

    public TimeOnly? UpdatedTime { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool Test { get; set; }

    public IList<IBaseEvent> DomainEvents { get; }
}