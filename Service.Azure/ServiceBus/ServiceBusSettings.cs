namespace Service.Azure.ServiceBus;

public record ServiceBusSettings
{
    public string ConfigurationKey = "ServiceBus";

    public string ConnectionString { get; set; } = default!;
    public string TranscriptQueue { get; set; } = default!;
}
