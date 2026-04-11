namespace BiUM.Core.Compensation;

public interface ICompensatableEntity
{
    public string? CStatus { get; set; }
}

public interface ICompensation : ICompensatableEntity;

public interface IReadableCompensation : ICompensatableEntity;