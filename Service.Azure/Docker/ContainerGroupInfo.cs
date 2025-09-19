namespace Service.Azure.Docker;

/// <summary>
/// Information about a container group and its state
/// </summary>
public class ContainerGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTimeOffset? CreatedTime { get; set; }
    public List<ContainerInfo> Containers { get; set; } = new();
}