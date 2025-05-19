namespace BiUM.Core.Common.Configs;

public class BiMailOptions
{
    public const string Name = "BiMailOptions";

    public required string TenantId { get; set; }

    public required string ClientId { get; set; }

    public required string ClientSecret { get; set; }
}