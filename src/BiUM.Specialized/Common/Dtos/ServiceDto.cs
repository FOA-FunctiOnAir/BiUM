using BiUM.Specialized.Common.Models;
using System.Data;

namespace BiUM.Specialized.Common.Dtos;

public class ServiceDto : BaseDto
{
    public required Guid MicroserviceId { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public required Guid HttpType { get; set; }
    public bool? IsExternal { get; set; }

    public virtual ICollection<ServiceParameterDto>? ServiceParameters { get; set; }
}

public class ServiceParameterDto : BaseDto
{
    public Guid? ServiceId { get; set; }
    public ParameterDirection? Direction { get; set; }
    public string? Property { get; set; }
    public Guid? FieldId { get; set; }
}