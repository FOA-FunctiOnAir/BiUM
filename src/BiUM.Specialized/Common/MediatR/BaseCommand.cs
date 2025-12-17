using BiUM.Specialized.Common.API;
using System;

namespace BiUM.Specialized.Common.MediatR;

public record BaseCommand<TType> : BaseRequestDto<TType>
{
    public Guid? Id { get; set; }
    public bool Test { get; set; }
}

public record BaseSchedulerCommand : BaseRequestDto<ApiEmptyResponse>
{
    public Guid? TenantId { get; set; }
}

public record BaseCommandDto : BaseCommand<ApiEmptyResponse>
{
}

public record BaseCommandResponseDto<TType> : BaseCommand<ApiResponse<TType>>
{
}

public record BaseImportCommandDto : BaseCommandDto
{
    public string Name { get; set; }
    public string Content { get; set; }
    public string MimeType { get; set; }
}