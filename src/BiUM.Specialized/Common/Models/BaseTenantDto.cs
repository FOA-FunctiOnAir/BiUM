using System;

namespace BiUM.Specialized.Common.Models;

public class BaseTenantDto : BaseDto
{
    public Guid TenantId { get; set; }
}