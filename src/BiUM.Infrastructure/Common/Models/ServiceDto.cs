using System;
using System.Collections.Generic;
using System.Data;

namespace BiUM.Infrastructure.Common.Models;

public class ServiceDto : BaseTenantDto
{
    public Guid MicroserviceId { get; set; }
    public string? MicroserviceRootPath { get; set; }
    public Guid Type { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public Guid HttpType { get; set; }
    public int? TimeoutMs { get; set; }
    public Guid? ServiceAuthenticationId { get; set; }
    public ServiceAuthenticationDto? Authentication { get; set; }

    public ICollection<ServiceParameterDto> ServiceParameters { get; set; } = [];
}

public class ServiceParameterDto : BaseDto
{
    public Guid? ServiceId { get; set; }
    public ParameterDirection? Direction { get; set; }
    public string? Property { get; set; }
    public Guid? FieldId { get; set; }
}

public class ServiceAuthenticationDto : BaseDto
{
    public Guid AuthType { get; set; }
    public string? TokenUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiKeyHeaderName { get; set; }
    public string? Audience { get; set; }
    public string? Scope { get; set; }
    public string? CustomHeadersJson { get; set; }
}
