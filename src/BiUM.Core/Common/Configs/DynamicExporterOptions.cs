namespace BiUM.Core.Common.Configs;

public class DynamicExporterOptions
{
    public const string Name = "DynamicExporterOptions";

    public int FetchPageSize { get; set; } = 2000;

    public int MaxExportRows { get; set; } = 1_000_000;

    public int MaxFetchPages { get; set; } = 500;

    public int PerPageTimeoutSeconds { get; set; } = 60;

    public int TotalJobTimeoutMinutes { get; set; } = 60;

    public int TtlDays { get; set; } = 3;

    public int SmallExportRowThreshold { get; set; } = 50_000;

    public string? ExportStoragePath { get; set; }
}