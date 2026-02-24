using BiUM.Contract.Models.Api;
using System;

namespace BiUM.Specialized.Common.MediatR;

public record BaseCommand<TType> : BaseRequestDto<TType>
{
    public Guid? Id { get; set; }
    public bool Test { get; set; }
}

public record BaseSchedulerCommand : BaseRequestDto<ApiResponse>
{
    public Guid? TenantId { get; set; }
}

public record BaseCommandDto : BaseCommand<ApiResponse>;

public record BaseCommandResponseDto<TType> : BaseCommand<ApiResponse<TType>>;

public record BaseImportCommandDto : BaseCommandDto
{
    public required string Name { get; set; }
    public required string Content { get; set; }
    public required string MimeType { get; set; }
}