namespace BiUM.Specialized.Common.Models;

public class BaseDto
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
}