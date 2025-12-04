namespace BiUM.Infrastructure.Services.Logging.Serilog;

public class SerilogOptions
{
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