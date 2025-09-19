namespace Service.Azure.Storage;

public class StorageSettings
{
    public string ConfigurationKey = "Storage";
    public string ApiVersion { get; set; } = default!;
    public string ConnectionString { get; set; } = default!;
}

