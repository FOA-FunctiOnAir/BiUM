using BiUM.Specialized.Common.MediatR;
using System;
using System.Collections.Generic;

namespace BiUM.Specialized.Common.DynamicExporter;

public record SaveExportRequestCommand
{
    public Guid? ApplicationId { get; set; }
    public required string Name { get; set; }
    public Guid? SourceMicroserviceId { get; set; }
    public required string SourceUrl { get; set; }
    public Dictionary<string, object?>? SourceParameters { get; set; }
    public string Format { get; set; } = "xlsx";
}

public record DeleteExportRequestCommand : BaseCommandDto;