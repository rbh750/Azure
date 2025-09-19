namespace Service.Azure.CosmosDb;

public class CosmosDbSettings
{
    public string ConfigurationKey = "CosmosDb";
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public List<CosmosContainer> Containers { get; set; } = [];
}

public class CosmosContainer
{
    public string? Reference { get; set; } = default!;
    public string? Id { get; set; } = default!;
    public string? DefaultPartitionKey { get; set; } = default!;
    public int? TimeToLIve { get; set; }
}

