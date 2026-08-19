using BiUM.Core.Compensation;
using BiUM.Infrastructure.Common.Models;

namespace BiUM.Tests.Helpers;

public sealed class TestCompensatableEntity : BaseEntity, ICompensation
{
    public Guid? CompensationSessionId { get; set; }
    public string? CStatus { get; set; }
    public string Name { get; set; } = string.Empty;
}