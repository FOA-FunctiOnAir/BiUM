using System;

namespace BiUM.Core.Compensation;

public interface ICompensatableEntity
{
    public Guid? CompensationSessionId { get; set; }
    public string? CStatus { get; set; }
}

public interface ICompensation : ICompensatableEntity;

public interface IReadableCompensation : ICompensatableEntity;