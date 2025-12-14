namespace BiUM.Core.Common.Configs;

public class SerilogOptions
{
    public const string Name = "SerilogOptions";

    public string? MinimumLevel { get; set; }
    public List<WriteToVm>? WriteTo { get; set; }

    public class WriteToVm
    {
        public string? Name { get; set; }
        public ArgsVm? Args { get; set; }
    }

    public class ArgsVm
    {
        public string? OutputTemplate { get; set; }
    }
}