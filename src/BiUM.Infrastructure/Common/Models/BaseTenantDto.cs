using System;

namespace BiUM.Infrastructure.Common.Models;

public class BaseTenantDto : BaseDto
{
    public Guid TenantId { get; set; }
}