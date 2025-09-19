using Newtonsoft.Json;

namespace Entities.Cosmos;

public class UserAccount
{
    [JsonProperty(PropertyName = "email")] public string PartitionKey { get; set; } = default!;
    [JsonProperty(PropertyName = "id")] public string Email { get; set; } = default!;
    [JsonProperty(PropertyName = "name")] public string Name { get; set; } = default!;
}