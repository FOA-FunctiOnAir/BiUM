using System;

namespace BiUM.Infrastructure.Common.Models;

public interface ITenantBaseEntity : IBaseEntity
{
    public Guid TenantId { get; set; }
}