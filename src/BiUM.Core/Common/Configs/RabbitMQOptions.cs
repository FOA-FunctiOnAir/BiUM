namespace BiUM.Core.Common.Configs;

public class RabbitMqOptions
{
    public const string Name = "RabbitMQOptions";

    public const string DefaultClientKey = "Default";

    public bool Enable { get; set; }
    public string? Hostname { get; set; }
    public int? Port { get; set; }
    public string? VirtualHost { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? Prefix { get; set; }
    public int ChannelPoolCapacity { get; set; } = 100;
    public int MaxRetryCount { get; set; } = 3;
    public bool DeadLetterQueueEnabled { get; set; }
    public int NetworkRecoveryIntervalSeconds { get; set; } = 5;
}